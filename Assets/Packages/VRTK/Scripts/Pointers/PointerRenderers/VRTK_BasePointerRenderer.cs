﻿// Base Pointer Renderer|PointerRenderers|10010
namespace VRTK
{
    using UnityEngine;
    using System.Collections.Generic;

    /// <summary>
    /// The Base Pointer Renderer script is an abstract class that handles the set up and operation of how a pointer renderer works.
    /// </summary>
    /// <remarks>
    /// As this is an abstract class, it cannot be applied directly to a game object and performs no logic.
    /// </remarks>
    public abstract class VRTK_BasePointerRenderer : MonoBehaviour
    {
        /// <summary>
        /// States of Pointer Visibility.
        /// </summary>
        /// <param name="On_When_Active">Only shows the object when the pointer is active.</param>
        /// <param name="Always_On">Ensures the object is always.</param>
        /// <param name="Always_Off">Ensures the object beam is never visible.</param>
        public enum VisibilityStates
        {
            OnWhenActive,
            AlwaysOn,
            AlwaysOff
        }

        [Header("General Renderer Settings")]

        [Tooltip("An optional Play Area Cursor generator to add to the destination position of the pointer tip.")]
        public VRTK_PlayAreaCursor playareaCursor;
        [Tooltip("The layers for the pointer's raycasts to ignore.")]
        public LayerMask layersToIgnore = Physics.IgnoreRaycastLayer;

        [Header("General Appearance Settings")]

        [Tooltip("The colour to change the pointer materials when the pointer collides with a valid object. Set to `Color.clear` to bypass changing material colour on valid collision.")]
        public Color validCollisionColor = Color.green;
        [Tooltip("The colour to change the pointer materials when the pointer is not colliding with anything or with an invalid object. Set to `Color.clear` to bypass changing material colour on invalid collision.")]
        public Color invalidCollisionColor = Color.red;

        [Tooltip("Determines when the main tracer of the pointer renderer will be visible.")]
        public VisibilityStates tracerVisibility = VisibilityStates.OnWhenActive;
        [Tooltip("Determines when the cursor/tip of the pointer renderer will be visible.")]
        public VisibilityStates cursorVisibility = VisibilityStates.OnWhenActive;

        protected const float BEAM_ADJUST_OFFSET = 0.00001f;

        protected VRTK_Pointer controllingPointer;
        protected RaycastHit destinationHit = new RaycastHit();
        protected Material defaultMaterial;
        protected Color currentColor;

        protected VRTK_PolicyList invalidListPolicy;
        protected float navMeshCheckDistance;
        protected bool headsetPositionCompensation;

        protected GameObject objectInteractor;
        protected GameObject objectInteractorAttachPoint;
        protected VRTK_InteractGrab controllerGrabScript;
        protected Rigidbody savedAttachPoint;
        protected bool attachedToInteractorAttachPoint = false;
        protected float savedBeamLength = 0f;
        protected List<GameObject> makeRendererVisible;

        /// <summary>
        /// The InitalizePointer method is used to set up the state of the pointer renderer.
        /// </summary>
        /// <param name="givenPointer">The VRTK_Pointer that is controlling the pointer renderer.</param>
        /// <param name="givenInvalidListPolicy">The VRTK_PolicyList for managing valid and invalid pointer locations.</param>
        /// <param name="givenNavMeshCheckDistance">The given distance from a nav mesh that the pointer can be to be valid.</param>
        /// <param name="givenHeadsetPositionCompensation">Determines whether the play area cursor will take the headset position within the play area into account when being displayed.</param>
        public virtual void InitalizePointer(VRTK_Pointer givenPointer, VRTK_PolicyList givenInvalidListPolicy, float givenNavMeshCheckDistance, bool givenHeadsetPositionCompensation)
        {
            controllingPointer = givenPointer;
            invalidListPolicy = givenInvalidListPolicy;
            navMeshCheckDistance = givenNavMeshCheckDistance;
            headsetPositionCompensation = givenHeadsetPositionCompensation;

            if (controllingPointer && controllingPointer.interactWithObjects && controllingPointer.controller && !objectInteractor)
            {
                controllerGrabScript = controllingPointer.controller.GetComponent<VRTK_InteractGrab>();
                CreateObjectInteractor();
            }
        }

        /// <summary>
        /// The Toggle Method is used to enable or disable the pointer renderer.
        /// </summary>
        /// <param name="pointerState">The activation state of the pointer.</param>
        /// <param name="actualState">The actual state of the activation button press.</param>
        public virtual void Toggle(bool pointerState, bool actualState)
        {
            if (controllingPointer && !pointerState)
            {
                controllingPointer.ResetActivationTimer();
                PointerExit(destinationHit);
            }
            ToggleObjectInteraction(pointerState);
            TogglePlayArea(pointerState, actualState);
            ToggleRenderer(pointerState, actualState);
        }

        /// <summary>
        /// The UpdateRenderer method is used to run an Update routine on the pointer.
        /// </summary>
        public virtual void UpdateRenderer()
        {
            if (playareaCursor && controllingPointer && controllingPointer.IsPointerActive())
            {
                playareaCursor.ToggleVisibility((destinationHit.transform != null));
            }
        }

        /// <summary>
        /// The GetDestinationHit method is used to get the RaycastHit of the pointer destination.
        /// </summary>
        /// <returns>The RaycastHit containing the information where the pointer is hitting.</returns>
        public virtual RaycastHit GetDestinationHit()
        {
            return destinationHit;
        }

        /// <summary>
        /// The ValidPlayArea method is used to determine if there is a valid play area and if it has had any collisions.
        /// </summary>
        /// <returns>Returns true if there is a valid play area and no collisions. Returns false if there is no valid play area or there is but with a collision detected.</returns>
        public virtual bool ValidPlayArea()
        {
            return (!playareaCursor || !playareaCursor.HasCollided());
        }

        protected abstract void CreatePointerObjects();
        protected abstract void DestroyPointerObjects();
        protected abstract void ToggleRenderer(bool pointerState, bool actualState);

        protected virtual void OnEnable()
        {
            defaultMaterial = Resources.Load("WorldPointer") as Material;
            makeRendererVisible = new List<GameObject>();
            CreatePointerObjects();
        }

        protected virtual void OnDisable()
        {
            DestroyPointerObjects();
            if (objectInteractor)
            {
                Destroy(objectInteractor);
            }
            controllerGrabScript = null;
        }

        protected virtual void FixedUpdate()
        {
            if (controllingPointer && controllingPointer.interactWithObjects && objectInteractor && objectInteractor.activeInHierarchy)
            {
                UpdateObjectInteractor();
            }
        }

        protected virtual void ToggleObjectInteraction(bool state)
        {
            if (controllingPointer && controllingPointer.interactWithObjects)
            {
                if (state && controllingPointer.grabToPointerTip && controllerGrabScript && objectInteractorAttachPoint)
                {
                    savedAttachPoint = controllerGrabScript.controllerAttachPoint;
                    controllerGrabScript.controllerAttachPoint = objectInteractorAttachPoint.GetComponent<Rigidbody>();
                    attachedToInteractorAttachPoint = true;
                }

                if (!state && controllingPointer.grabToPointerTip && controllerGrabScript)
                {
                    if (attachedToInteractorAttachPoint)
                    {
                        controllerGrabScript.ForceRelease(true);
                    }
                    controllerGrabScript.controllerAttachPoint = savedAttachPoint;
                    savedAttachPoint = null;
                    attachedToInteractorAttachPoint = false;
                    savedBeamLength = 0f;
                }

                if (objectInteractor)
                {
                    objectInteractor.SetActive(state);
                }
            }
        }

        protected virtual void UpdateObjectInteractor()
        {
            objectInteractor.transform.position = destinationHit.point;
        }

        protected virtual Vector3 GetOriginPosition()
        {
            return (controllingPointer.customOrigin ? controllingPointer.customOrigin.position : controllingPointer.transform.position);
        }

        protected virtual Vector3 GetOriginLocalPosition()
        {
            return (controllingPointer.customOrigin ? controllingPointer.customOrigin.localPosition : Vector3.zero);
        }

        protected virtual Vector3 GetOriginForward()
        {
            return (controllingPointer.customOrigin ? controllingPointer.customOrigin.forward : controllingPointer.transform.forward);
        }

        protected virtual Quaternion GetOriginRotation()
        {
            return (controllingPointer.customOrigin ? controllingPointer.customOrigin.rotation : controllingPointer.transform.rotation);
        }

        protected virtual Quaternion GetOriginLocalRotation()
        {
            return (controllingPointer.customOrigin ? controllingPointer.customOrigin.localRotation : Quaternion.identity);
        }

        protected virtual void PointerEnter(RaycastHit givenHit)
        {
            controllingPointer.PointerEnter(givenHit);
        }

        protected virtual void PointerExit(RaycastHit givenHit)
        {
            controllingPointer.PointerExit(givenHit);
        }

        protected virtual bool ValidDestination()
        {
            bool validNavMeshLocation = false;
            if (destinationHit.transform)
            {
                UnityEngine.AI.NavMeshHit hit;
                validNavMeshLocation = UnityEngine.AI.NavMesh.SamplePosition(destinationHit.point, out hit, navMeshCheckDistance, UnityEngine.AI.NavMesh.AllAreas);
            }
            if (navMeshCheckDistance == 0f)
            {
                validNavMeshLocation = true;
            }
            return (validNavMeshLocation && destinationHit.transform && !(VRTK_PolicyList.Check(destinationHit.transform.gameObject, invalidListPolicy)));
        }

        protected virtual void TogglePlayArea(bool pointerState, bool actualState)
        {
            if (playareaCursor)
            {
                playareaCursor.SetHeadsetPositionCompensation(headsetPositionCompensation);
                playareaCursor.ToggleState(pointerState);
            }

            if (playareaCursor && pointerState)
            {
                if (actualState)
                {
                    ToggleRendererVisibility(playareaCursor.GetPlayAreaContainer(), false);
                    AddVisibleRenderer(playareaCursor.GetPlayAreaContainer());
                }
                else
                {
                    ToggleRendererVisibility(playareaCursor.GetPlayAreaContainer(), true);
                }
            }
        }

        protected virtual void ToggleElement(GameObject givenObject, bool pointerState, bool actualState, VisibilityStates givenVisibility, ref bool currentVisible)
        {
            if (givenObject)
            {
                currentVisible = (givenVisibility == VisibilityStates.AlwaysOn ? true : pointerState);

                givenObject.SetActive(currentVisible);

                if (givenVisibility == VisibilityStates.AlwaysOff)
                {
                    ToggleRendererVisibility(givenObject, false);
                }
                else
                {
                    if (actualState)
                    {
                        ToggleRendererVisibility(givenObject, false);
                        AddVisibleRenderer(givenObject);
                    }
                    else
                    {
                        ToggleRendererVisibility(givenObject, true);
                    }
                }
            }
        }

        protected virtual void AddVisibleRenderer(GameObject givenObject)
        {
            if (!makeRendererVisible.Contains(givenObject))
            {
                makeRendererVisible.Add(givenObject);
            }
        }

        protected virtual void MakeRenderersVisible()
        {
            for (int i = 0; i < makeRendererVisible.Count; i++)
            {
                ToggleRendererVisibility(makeRendererVisible[i], true);
                makeRendererVisible.Remove(makeRendererVisible[i]);
            }
        }

        protected virtual void ToggleRendererVisibility(GameObject givenObject, bool state)
        {
            Renderer[] renderers = givenObject.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = state;
            }
        }

        protected virtual void SetupMaterialRenderer(GameObject givenObject)
        {
            var pointerRenderer = givenObject.GetComponent<MeshRenderer>();
            pointerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pointerRenderer.receiveShadows = false;
            pointerRenderer.material = defaultMaterial;
        }

        protected virtual void ChangeColor(Color givenColor)
        {
            if ((playareaCursor && playareaCursor.HasCollided()) || !ValidDestination())
            {
                givenColor = invalidCollisionColor;
            }

            if (givenColor != Color.clear)
            {
                currentColor = givenColor;
                ChangeMaterial(givenColor);
            }
        }

        protected virtual void ChangeMaterial(Color givenColor)
        {
            if (playareaCursor)
            {
                playareaCursor.SetMaterialColor(givenColor);
            }
        }

        protected virtual void ChangeMaterialColor(GameObject givenObject, Color givenColor)
        {
            if (givenObject)
            {
                Renderer[] foundRenderers = givenObject.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < foundRenderers.Length; i++)
                {
                    Renderer foundRenderer = foundRenderers[i];
                    if (foundRenderer.material)
                    {
                        foundRenderer.material.EnableKeyword("_EMISSION");

                        if (foundRenderer.material.HasProperty("_Color"))
                        {
                            foundRenderer.material.color = givenColor;
                        }

                        if (foundRenderer.material.HasProperty("_EmissionColor"))
                        {
                            foundRenderer.material.SetColor("_EmissionColor", VRTK_SharedMethods.ColorDarken(givenColor, 50));
                        }
                    }
                }
            }
        }

        protected virtual void CreateObjectInteractor()
        {
            objectInteractor = new GameObject(string.Format("[{0}]BasePointerRenderer_ObjectInteractor_Container", gameObject.name));
            objectInteractor.transform.SetParent(controllingPointer.controller.transform);
            objectInteractor.transform.localPosition = Vector3.zero;
            objectInteractor.layer = LayerMask.NameToLayer("Ignore Raycast");
            VRTK_PlayerObject.SetPlayerObject(objectInteractor, VRTK_PlayerObject.ObjectTypes.Pointer);

            var objectInteractorCollider = new GameObject(string.Format("[{0}]BasePointerRenderer_ObjectInteractor_Collider", gameObject.name));
            objectInteractorCollider.transform.SetParent(objectInteractor.transform);
            objectInteractorCollider.transform.localPosition = Vector3.zero;
            objectInteractorCollider.layer = LayerMask.NameToLayer("Ignore Raycast");
            var tmpCollider = objectInteractorCollider.AddComponent<SphereCollider>();
            tmpCollider.isTrigger = true;
            VRTK_PlayerObject.SetPlayerObject(objectInteractorCollider, VRTK_PlayerObject.ObjectTypes.Pointer);

            if (controllingPointer.grabToPointerTip)
            {
                objectInteractorAttachPoint = new GameObject(string.Format("[{0}]BasePointerRenderer_ObjectInteractor_AttachPoint", gameObject.name));
                objectInteractorAttachPoint.transform.SetParent(objectInteractor.transform);
                objectInteractorAttachPoint.transform.localPosition = Vector3.zero;
                objectInteractorAttachPoint.layer = LayerMask.NameToLayer("Ignore Raycast");
                var objectInteratorRigidBody = objectInteractorAttachPoint.AddComponent<Rigidbody>();
                objectInteratorRigidBody.isKinematic = true;
                objectInteratorRigidBody.freezeRotation = true;
                objectInteratorRigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                VRTK_PlayerObject.SetPlayerObject(objectInteractorAttachPoint, VRTK_PlayerObject.ObjectTypes.Pointer);
            }

            ScaleObjectInteractor(Vector3.one * 0.025f);
            objectInteractor.SetActive(false);
        }

        protected virtual void ScaleObjectInteractor(Vector3 scaleAmount)
        {
            if (objectInteractor)
            {
                objectInteractor.transform.localScale = scaleAmount;
            }
        }

        protected virtual float OverrideBeamLength(float currentLength)
        {
            if (!controllerGrabScript || !controllerGrabScript.GetGrabbedObject())
            {
                savedBeamLength = 0f;
            }

            if (controllingPointer && controllingPointer.interactWithObjects && controllingPointer.grabToPointerTip && attachedToInteractorAttachPoint && controllerGrabScript && controllerGrabScript.GetGrabbedObject())
            {
                savedBeamLength = (savedBeamLength == 0f ? currentLength : savedBeamLength);
                return savedBeamLength;
            }
            return currentLength;
        }

        protected virtual void UpdateDependencies(Vector3 location)
        {
            if (playareaCursor)
            {
                playareaCursor.SetPlayAreaCursorTransform(location);
            }
        }
    }
}