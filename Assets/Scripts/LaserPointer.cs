using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(SteamVR_TrackedObject))]
public class LaserPointer : MonoBehaviour
{
    // Event Declaration Code
    public struct PointerEventArgs
    {
        public uint controllerIndex;
        public uint flags;
        public float distance;
        public Transform target;
        public Vector2 textureCoord;
        public float angle;
    }

    public delegate void PointerEventHandler(object sender, PointerEventArgs e);

    [System.Serializable]
    public class PointerEvent : UnityEvent<PointerEventArgs> { }

    // Native C# Events are more efficient but can only be used from code
    public event PointerEventHandler PointerIn;
    public event PointerEventHandler PointerOut;
    public event PointerEventHandler PointerUpdate;
    public event PointerEventHandler InvPointerIn;
    public event PointerEventHandler InvPointerOut;
    public event PointerEventHandler InvPointerUpdate;


    // Unity Events add a little overhead but listeners can be assigned via Unity editor
    public PointerEvent unityPointerIn = new PointerEvent();
    public PointerEvent unityPointerOut = new PointerEvent();
    public PointerEvent unityPointerUpdate = new PointerEvent();
    public PointerEvent unityInvPointerIn = new PointerEvent();
    public PointerEvent unityInvPointerOut = new PointerEvent();
    public PointerEvent unityInvPointerUpdate = new PointerEvent();

    // Member Variables
    public Color color = new Color(0.0F, 1.0F, .01F);
    public float thickness = 0.002F;
    public float thicknessPressed = 0.01F;
    [Tooltip("CAUTION: GO assigned here will be enabled/disabled by this script")]
    public GameObject parent;
    public GameObject pointerModel;
    public bool addRigidBody = false;

    protected Transform previousContact = null;

    private SteamVR_TrackedObject controller;
    private Vector2 prevTextureCoord;
    private float prevDistance;
    private Transform previousInvContact;
    private float prevInvDistance;
    private Vector2 prevInvTextureCoord;

    // Unity lifecycle method
    public void Awake()
    {
        controller = GetComponent<SteamVR_TrackedObject>();

        // Make sure the Laser Pointer has a Parent
        if (parent == null)
        {
            parent = new GameObject("Laser Pointer Container");
            parent.transform.parent = this.transform;
            parent.transform.localPosition = Vector3.zero;
            parent.transform.localRotation = Quaternion.identity;
        }
        parent.gameObject.SetActive(false); // Disabled until OnEnable() is called

        // Create a pointer GO & Model if none was assigned in the inspector
        if (pointerModel == null)
        {
            // Using a cube to save poly's. Since it's 'unlit' the player cannot distinguish between a 
            // cube and a cylinder
            pointerModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pointerModel.name = "Pointer Model";
            pointerModel.transform.parent = parent.transform;
            pointerModel.transform.localPosition = new Vector3(0f, 0f, 50f);
            pointerModel.transform.localRotation = Quaternion.identity;

            /*
             * This requires that you have the shader Unlit/Color in the build.
             * To make sure this is the case, follow the instructions in the
             * manual: http://docs.unity3d.com/ScriptReference/Shader.Find.html
             */
            Material newMaterial = new Material(Shader.Find("Unlit/Color"));
            pointerModel.GetComponent<MeshRenderer>().material = newMaterial;

        }
        pointerModel.transform.localScale = new Vector3(thickness, thickness, 100f);
        pointerModel.GetComponent<MeshRenderer>().material.SetColor("_Color", color);

        BoxCollider collider = pointerModel.GetComponent<BoxCollider>();
        if (addRigidBody)
        {
            if (collider)
            {
                collider.isTrigger = true;
            }
            pointerModel.AddComponent<Rigidbody>();
            collider.attachedRigidbody.useGravity = false;
            collider.attachedRigidbody.isKinematic = true;
        }
        else
        {
            if (collider)
            {
                Object.Destroy(collider);
            }
        }
    }

    // Unity lifecycle method
    public void OnEnable()
    {
        parent.gameObject.SetActive(true);
        SteamVR_Utils.Event.Listen("input_focus", OnInputFocus);
    }

    // Unity lifecycle method
    public void OnDisable()
    {
        parent.gameObject.SetActive(false);
        SteamVR_Utils.Event.Remove("input_focus", OnInputFocus);
    }

    // Handle the Steam Dashboard
    private void OnInputFocus(params object[] args)
    {
        bool hasFocus = (bool)args[0];
        if (hasFocus)
        {
            parent.gameObject.SetActive(this.enabled);
        }
        else
        {
            parent.gameObject.SetActive(false);
        }
    }

    // Unity lifecycle method
    void Update()
    {
        float dist = 100f;
        float invdist = 100f;

        Ray raycast = new Ray(transform.position, transform.forward);
        Ray invraycast = new Ray(transform.position, -transform.forward);
        RaycastHit hitInfo;
        RaycastHit invHitInfo;
        bool hasTarget = Physics.Raycast(raycast, out hitInfo);
        bool invTarget = Physics.Raycast(invraycast, out invHitInfo);

        // Pointer moved off collider
        if (previousContact && previousContact != hitInfo.transform)
        {
            PointerEventArgs argsOut = new PointerEventArgs();
            if (controller != null)
            {
                argsOut.controllerIndex = (uint)controller.index;
            }
            argsOut.distance = 0f;
            argsOut.flags = 0;
            argsOut.target = previousContact;
            argsOut.textureCoord = hitInfo.textureCoord;
            argsOut.angle = 0f;
            OnPointerOut(argsOut);
            previousContact = null;
        }

        // Pointer moved off collider
        if (previousInvContact && previousInvContact != invHitInfo.transform)
        {
            PointerEventArgs argsOut = new PointerEventArgs();
            if (controller != null)
            {
                argsOut.controllerIndex = (uint)controller.index;
            }
            argsOut.distance = 0f;
            argsOut.flags = 0;
            argsOut.target = previousInvContact;
            argsOut.textureCoord = invHitInfo.textureCoord;
            argsOut.angle = 0f;
            OnInvPointerOut(argsOut);
            previousInvContact = null;
        }
        // Point moved onto new collider
        if (hasTarget && previousContact != hitInfo.transform)
        {
            PointerEventArgs argsIn = new PointerEventArgs();
            if (controller != null)
            {
                argsIn.controllerIndex = (uint)controller.index;
            }
            argsIn.distance = hitInfo.distance;
            argsIn.flags = 0;
            argsIn.target = hitInfo.transform;
            argsIn.textureCoord = hitInfo.textureCoord;
            argsIn.angle = Vector3.Angle(hitInfo.point - this.transform.position, hitInfo.normal);
            OnPointerIn(argsIn);
            prevDistance = hitInfo.distance;
            prevTextureCoord = hitInfo.textureCoord;
            previousContact = hitInfo.transform; 
        }

        if (invTarget && previousInvContact != invHitInfo.transform)
        {
            PointerEventArgs argsIn = new PointerEventArgs();
            if (controller != null)
            {
                argsIn.controllerIndex = (uint)controller.index;
            }
            argsIn.distance = invHitInfo.distance;
            argsIn.flags = 0;
            argsIn.target = invHitInfo.transform;
            argsIn.textureCoord = invHitInfo.textureCoord;
            argsIn.angle = Vector3.Angle(hitInfo.point - this.transform.position, hitInfo.normal);
            OnInvPointerIn(argsIn);
            prevInvDistance = invHitInfo.distance;
            prevInvTextureCoord = invHitInfo.textureCoord;
            previousInvContact = invHitInfo.transform;
        }


        // Different hit coordinate or distance but same contact
        if (hasTarget &&
            (hitInfo.distance != prevDistance || hitInfo.textureCoord != prevTextureCoord) &&
            previousContact == hitInfo.transform)
        {
            PointerEventArgs argsUpdate = new PointerEventArgs();
            if (controller != null)
            {
                argsUpdate.controllerIndex = (uint)controller.index;
            }
            argsUpdate.distance = hitInfo.distance;
            argsUpdate.flags = 0;
            argsUpdate.target = hitInfo.transform;
            argsUpdate.textureCoord = hitInfo.textureCoord;
            argsUpdate.angle = Vector3.Angle(hitInfo.point - this.transform.position, hitInfo.normal);
            OnPointerUpdate(argsUpdate);
        }

        if (invTarget &&
    (invHitInfo.distance != prevInvDistance || invHitInfo.textureCoord != prevInvTextureCoord) &&
    previousInvContact == invHitInfo.transform)
        {
            PointerEventArgs argsUpdate = new PointerEventArgs();
            if (controller != null)
            {
                argsUpdate.controllerIndex = (uint)controller.index;
            }
            argsUpdate.distance = invHitInfo.distance;
            argsUpdate.flags = 0;
            argsUpdate.target = invHitInfo.transform;
            argsUpdate.textureCoord = invHitInfo.textureCoord;
            argsUpdate.angle = Vector3.Angle(hitInfo.point - this.transform.position, hitInfo.normal);
            OnInvPointerUpdate(argsUpdate);
        }

        if (!hasTarget)
        {
            previousContact = null;
        }
        else if (hitInfo.distance < 100f)
        {
            dist = hitInfo.distance;
        }
        if (!invTarget)
        {
            previousInvContact = null;
        }
        else if (invHitInfo.distance < 100f)
        {
            invdist = invHitInfo.distance;
        }

        pointerModel.transform.localPosition = new Vector3(0f, 0f, dist / 2f);

        //float currentThickness = controller != null && device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger)
        //    ? thicknessPressed : thickness;
        //pointerModel.transform.localScale = new Vector3(currentThickness, currentThickness, dist);
    }

    // Event publisher
    public virtual void OnPointerIn(PointerEventArgs e)
    {
        if (PointerIn != null)
        {
            PointerIn(this, e);
        }
        unityPointerIn.Invoke(e);
    }

    // Event publisher
    public virtual void OnPointerOut(PointerEventArgs e)
    {
        if (PointerOut != null)
        {
            PointerOut(this, e);
        }
        unityPointerOut.Invoke(e);
    }

    // Event publisher
    public virtual void OnPointerUpdate(PointerEventArgs e)
    {
        if (PointerUpdate != null)
        {
            PointerUpdate(this, e);
        }
        unityPointerUpdate.Invoke(e);
    }

    public virtual void OnInvPointerIn(PointerEventArgs e)
    {
        if (InvPointerIn != null)
        {
            InvPointerIn(this, e);
        }
        unityInvPointerIn.Invoke(e);
    }

    public virtual void OnInvPointerOut(PointerEventArgs e)
    {
        if (InvPointerOut != null)
        {
            InvPointerOut(this, e);
        }
        unityInvPointerOut.Invoke(e);
    }

    public virtual void OnInvPointerUpdate(PointerEventArgs e)
    {
        if (InvPointerUpdate != null)
        {
            InvPointerUpdate(this, e);
        }
        unityInvPointerUpdate.Invoke(e);
    }

}