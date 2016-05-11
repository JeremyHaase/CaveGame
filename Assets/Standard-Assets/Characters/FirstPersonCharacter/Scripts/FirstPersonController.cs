using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (CharacterController))]
    [RequireComponent(typeof (AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;
        
        [SerializeField] private AudioSource externalSound;
        [SerializeField] private AudioClip doorBreak;
        [SerializeField] private AudioClip monster;
        [SerializeField] private Image loseScreen;
        [SerializeField] private Image winScreen;
        public Text activationCommand;
        public GameObject lamp;
        public GameObject otherLamp;
        public Text info;
        public Text noteInfo;
        public GameObject axe;
        public GameObject otherAxe;
        private Inventory1 inventory;
        private Animation objectMotion;
        private IsDoorOpen doorOpen;
        private bool firstLockedDoor;
        private float speed;
        private RaycastHit hit;
        public Canvas canvas;
        private float timeElapsed;
        private float timeLeft;

        // Use this for initialization
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);

            timeElapsed = 0;
            timeLeft = 900;
            lamp.SetActive(false);
            axe.SetActive(false);
            activationCommand.text = "";
            info.text = "";
            noteInfo.text = "";
            inventory = gameObject.GetComponent<Inventory1>();
            firstLockedDoor = true;
        }


        // Update is called once per frame
        private void Update()
        {
            RotateView();
            // the jump state needs to read here to make sure it is not missed
            /*if (!m_Jump)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }*/

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;

            //RaycastHit hit;
            Ray ray = m_Camera.ViewportPointToRay(new Vector3(0.5F, 0.5F, 0));

            if (Physics.Raycast(ray, out hit) && Physics.Raycast(ray, 1.5F))
            {
                if (hit.transform.tag == "lamp" || hit.transform.tag == "axe" || hit.transform.tag == "key")
                {
                    activationCommand.text = "Left Click to Pick Up";
                }
                else if(hit.transform.tag == "door" || hit.transform.tag == "doubledoor")
                {
                    activationCommand.text = axe.activeSelf ? "Left Click to Destroy with Axe" : "Left Click to Open";
                }
                else if (hit.transform.tag == "note")
                {
                    activationCommand.text = "Left Click to Read";
                }

            }
            else
            {
                activationCommand.text = "";
            }

            if (activationCommand.text != "")
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (hit.transform.tag == "lamp")
                    {
                        lamp.SetActive(true);
                        axe.SetActive(false);
                        otherLamp.SetActive(false);
                        inventory.hasLamp = true;
                        activationCommand.text = "";
                        info.text = "Toggle Lamp with '1'";
                        Invoke("ResetInfoText", 3);
                    }
                    else if (hit.transform.tag == "axe")
                    {
                        axe.SetActive(true);
                        lamp.SetActive(false);
                        otherAxe.SetActive(false);
                        inventory.hasAxe = true;
                        activationCommand.text = "";
                        info.text = "Toggle Axe with '2'";
                        Invoke("ResetInfoText", 3);
                    }
                    else if (hit.transform.tag == "note")
                    {
                        canvas.gameObject.transform.FindChild("Image").gameObject.SetActive(true);
                        noteInfo.text = "Right Click to Exit";
                        
                        if (hit.transform.name == "AxeNote")
                        {
                            canvas.gameObject.transform.FindChild("AxeNote").gameObject.SetActive(true);
                        }
                        else if (hit.transform.name == "WarehouseNote")
                        {
                            canvas.gameObject.transform.FindChild("WarehouseNote").gameObject.SetActive(true);
                        }
                        else if (hit.transform.name == "PreacherHousePreacherNote")
                        {
                            canvas.gameObject.transform.FindChild("PreacherHousePreacherNote").gameObject.SetActive(true);
                        }
                        else if (hit.transform.name == "PreacherHouseTerryNote")
                        {
                            canvas.gameObject.transform.FindChild("PreacherHouseTerryNote").gameObject.SetActive(true);
                        }
                        else if (hit.transform.name == "ChurchNote")
                        {
                            canvas.gameObject.transform.FindChild("ChurchNote").gameObject.SetActive(true);
                        }
                        else if (hit.transform.name == "CarpenterHouseNote")
                        {
                            canvas.gameObject.transform.FindChild("CarpenterHouseNote").gameObject.SetActive(true);
                        }
                        else if (hit.transform.name == "WellNote")
                        {
                            canvas.gameObject.transform.FindChild("WellNote").gameObject.SetActive(true);
                        }
                        else
                        {
                            canvas.gameObject.transform.FindChild("SupplyStoreNote").gameObject.SetActive(true);
                        }
                    }
                    else if (hit.transform.tag == "key")
                    {
                        if (hit.transform.name == "GateKey")
                        {
                            inventory.hasGateKey = true;
                        }
                        else if (hit.transform.name == "ChurchKey")
                        {
                            inventory.hasChurchKey = true;
                        }
                        else if (hit.transform.name == "CarpenterHouseKey")
                        {
                            inventory.hasCarpenterHouseKey = true;
                        }
                        else if (hit.transform.name == "GrocerHouseKey")
                        {
                            inventory.hasGrocerHouseKey = true;
                        }
                        else if (hit.transform.name == "GroceryStoreKey")
                        {
                            inventory.hasGroceryStoreKey = true;
                        }
                        else if (hit.transform.name == "MansionKey")
                        {
                            inventory.hasMansionKey = true;
                        }
                        else if (hit.transform.name == "SupplyStoreKey")
                        {
                            inventory.hasSupplyStoreKey = true;
                        }
                        else //if (hit.transform.name == "WarehouseKey")
                        {
                            inventory.hasWarehouseKey = true;
                        }

                        hit.transform.gameObject.SetActive(false);

                    }
                    else if (axe.activeSelf && hit.transform.name != "GateDoor")
                    {
                        objectMotion = axe.GetComponent<Animation>();
                        objectMotion.Play();
                        Invoke("DeactivateObject", 1);
                        m_AudioSource.volume = 1F;
                        m_AudioSource.clip = doorBreak;
                        m_AudioSource.PlayOneShot(doorBreak);
                        timeLeft = timeLeft / 2;
                    }
                    else if (axe.activeSelf)
                    {
                        info.text = "Nice try, but seriously go find the Key.";
                        Invoke("ResetInfoText", 3);
                    }
                    else
                    {
                        if (hit.transform.tag == "door")
                        {
                            objectMotion = hit.transform.GetComponentInParent<Animation>();
                            doorOpen = hit.transform.GetComponentInParent<IsDoorOpen>();

                            if (hit.transform.name == "MansionDoor" && inventory.hasMansionKey)
                            { 
                                doorOpen.unlocked = true;
                            } 
                            else if(hit.transform.name == "GroceryStoreDoor" && inventory.hasGroceryStoreKey)
                            {
                                doorOpen.unlocked = true;
                            }
                            else if(hit.transform.name == "SupplyStoreDoor" && inventory.hasSupplyStoreKey)
                            {
                                doorOpen.unlocked = true;
                            }
                            else if(hit.transform.name == "ChurchDoor" && inventory.hasChurchKey)
                            {
                                doorOpen.unlocked = true;
                            }
                            else if(hit.transform.name == "GrocerHouseDoor" && inventory.hasGrocerHouseKey)
                            {
                                doorOpen.unlocked = true;
                            }
                            else if(hit.transform.name == "CarpenterHouseDoor" && inventory.hasCarpenterHouseKey)
                            {
                                doorOpen.unlocked = true;
                            }
                            
                            if (doorOpen.unlocked)
                            {
                                if (!(doorOpen.open))
                                {
                                    objectMotion["DoorAnimation"].speed = 1;
                                    objectMotion.Play();
                                    doorOpen.open = true;
                                }
                                else
                                {
                                    objectMotion["DoorAnimation"].speed = -1;
                                    objectMotion["DoorAnimation"].time = objectMotion["DoorAnimation"].length;
                                    objectMotion.Play();
                                    doorOpen.open = false;
                                }
                            }
                            else
                            {
                                if (firstLockedDoor)
                                {
                                    info.text = "Locked:\nLocked Doors can be opened with a key or smashed with an axe.";
                                    firstLockedDoor = false;
                                    Invoke("ResetInfoText", 5);
                                }
                                else
                                {
                                    info.text = "Locked";
                                    Invoke("ResetInfoText", 1);
                                }
                            }
                        }
                        else if (hit.transform.tag == "doubledoor")
                        {
                            objectMotion = hit.transform.parent.GetComponentInParent<Animation>();
                            doorOpen = hit.transform.parent.GetComponentInParent<IsDoorOpen>();

                            if (hit.transform.name == "WarehouseDoor" && inventory.hasWarehouseKey)
                            {
                                doorOpen.unlocked = true;
                            }
                            else if (hit.transform.name == "GateDoor" && inventory.hasGateKey)
                            {
                                doorOpen.unlocked = true;
                            } 
                            
                            if (doorOpen.unlocked)
                            {
                                if (!(doorOpen.open))
                                {
                                    objectMotion["DoubleDoorAnimation"].speed = 1;
                                    objectMotion.Play();
                                    doorOpen.open = true;
                                }
                                else
                                {
                                    objectMotion["DoubleDoorAnimation"].speed = -1;
                                    objectMotion["DoubleDoorAnimation"].time = objectMotion["DoubleDoorAnimation"].length;
                                    objectMotion.Play();
                                    doorOpen.open = false;
                                }
                                if (hit.transform.name == "GateDoor")
                                {
                                    externalSound = hit.transform.parent.GetComponentInParent<AudioSource>();
                                    externalSound.Play();
                                }
                            }
                            else
                            {
                                if (firstLockedDoor)
                                {
                                    info.text = "Locked:\nLocked Doors can be opened with a key or smashed with an axe.";
                                    firstLockedDoor = false;
                                    Invoke("ResetInfoText", 5);
                                }
                                else
                                {
                                    info.text = "Locked";
                                    Invoke("ResetInfoText", 1);
                                }
                            }
                        }
                    }
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                canvas.gameObject.transform.FindChild("Image").gameObject.SetActive(false);
                canvas.gameObject.transform.FindChild("AxeNote").gameObject.SetActive(false);
                canvas.gameObject.transform.FindChild("WarehouseNote").gameObject.SetActive(false);
                canvas.gameObject.transform.FindChild("PreacherHousePreacherNote").gameObject.SetActive(false);
                canvas.gameObject.transform.FindChild("PreacherHouseTerryNote").gameObject.SetActive(false);
                canvas.gameObject.transform.FindChild("ChurchNote").gameObject.SetActive(false);
                canvas.gameObject.transform.FindChild("CarpenterHouseNote").gameObject.SetActive(false);
                canvas.gameObject.transform.FindChild("WellNote").gameObject.SetActive(false);
                canvas.gameObject.transform.FindChild("SupplyStoreNote").gameObject.SetActive(false);
                noteInfo.text = "";
            }

            if (inventory.hasLamp == true && Input.GetKeyDown("1"))
            {
                
                if (lamp.activeSelf == false)
                {
                    lamp.SetActive(true);
                    axe.SetActive(false);
                }
                else
                {
                    lamp.SetActive(false);
                }
            } else if (inventory.hasAxe == true && Input.GetKeyDown("2"))
            {

                if (axe.activeSelf == false)
                {
                    axe.SetActive(true);
                    lamp.SetActive(false);
                }
                else
                {
                    axe.SetActive(false);
                }
            }


        }


        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void FixedUpdate()
        {
            //float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height / 2f);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.x = desiredMove.x * speed;
            m_MoveDir.z = desiredMove.z * speed;


            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else
            {
                m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);

            timeElapsed = Time.fixedTime;
            if (timeElapsed >= timeLeft && !(winScreen.gameObject.activeSelf))
            {
                m_AudioSource.volume = 1F;
                m_AudioSource.clip = monster;
                m_AudioSource.PlayOneShot(monster);
                loseScreen.gameObject.SetActive(true);
            }
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }

        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            if (!m_IsWalking) 
            {
                m_AudioSource.volume = 0.2F;

            }
            else
            {
                m_AudioSource.volume = 0.05F;
            }
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }


        private void RotateView()
        {
            m_MouseLook.LookRotation (transform, m_Camera.transform);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
        }

        private void ResetInfoText()
        {
            info.text = "";
        }

        private void DeactivateObject()
        {
            if (hit.transform.tag == "door" || hit.transform.tag == "doubledoor")
            {
                hit.transform.gameObject.SetActive(false);
            }
        }
    }
}
