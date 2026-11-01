using UnityEngine;
using UnityEngine.InputSystem;
public class Movement : MonoBehaviour
{
   // PARAMETERS - for tuning, typically set in the editor
   // CACHE - e.g. references for readability or speed
   // STATE - private instance (member) variables
   [SerializeField] InputAction thrust;
   [SerializeField] InputAction rotation;
   [SerializeField] float thrustStrength = 100f;
   [SerializeField] float rotationStrength = 100f;
   [SerializeField] AudioClip mainEngineSFX;
   [SerializeField] ParticleSystem mainEngineParticles;
   [SerializeField] ParticleSystem rightThrustParticles;
   [SerializeField] ParticleSystem leftThrustParticles;

   Rigidbody rb;
   AudioSource audioSource;
   FuelTank fuelTank;

   private void Start()
   {
      rb = GetComponent<Rigidbody>();
      audioSource = GetComponent<AudioSource>();
      fuelTank = GetComponent<FuelTank>();
   }

   private void OnEnable()  
    {
       thrust.Enable();
       rotation.Enable();
    }

   private void FixedUpdate()
   {
      ProcessThrust();
      ProcessRotation();
   }

   private void ProcessThrust()
   {
      if (thrust.IsPressed() && fuelTank.HasFuel)
      {
         StartThrusting();
         fuelTank.Burn(Time.fixedDeltaTime);
      }
      else
      {
         StopThrusting();
      }
   }
   private void StartThrusting()
   {
      rb.AddRelativeForce(Vector3.up * thrustStrength * Time.fixedDeltaTime);
      if (!audioSource.isPlaying)
      {
         audioSource.PlayOneShot(mainEngineSFX);
      }
      if (!mainEngineParticles.isPlaying)
      {
         mainEngineParticles.Play();
      }
   }
   private void StopThrusting()
   {
      audioSource.Stop();
      mainEngineParticles.Stop();
   }
   

   private void ProcessRotation()
   {
      float rotationInput = rotation.ReadValue<float>();
      if (rotationInput < 0)
      {
         RotateRight();
      }
      else if (rotationInput > 0)
      {
         RotateLeft();
      }
      else
      {
         StopRotating();
      }
   }
   private void RotateRight()
   {
      ApplyRotation(rotationStrength);
      if (!rightThrustParticles.isPlaying)
      {
         leftThrustParticles.Stop();
         rightThrustParticles.Play();
      }
   }
   private void RotateLeft()
   {
      ApplyRotation(-rotationStrength);
      if (!leftThrustParticles.isPlaying)
      {
         rightThrustParticles.Stop();
         leftThrustParticles.Play();
      }
   }
   private void StopRotating()
   {
      rightThrustParticles.Stop();
      leftThrustParticles.Stop();
   }
   
   private void ApplyRotation(float rotationThisFrame)
   {
      rb.freezeRotation = true;
      transform.Rotate(Vector3.forward * rotationThisFrame * Time.fixedDeltaTime);
      rb.freezeRotation = false;
   }
}
