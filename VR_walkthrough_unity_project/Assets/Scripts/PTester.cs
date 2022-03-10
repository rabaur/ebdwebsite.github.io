using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PTester : MonoBehaviour
{
    public float spawnVolumeSize = 10.0f;
    public int particleCount = 1000;
    public Gradient gradient;

    private Vector3[] particlePos;
    private ParticleSystem particleSys;
    private ParticleSystem.Particle[] particles;

    // Start is called before the first frame update
    void Start()
    {
        // Generate random positions.
        particlePos = new Vector3[particleCount];
        for (int i = 0; i < particlePos.Length; i++)
        {
            float randX = Random.value * spawnVolumeSize;
            float randY = Random.value * spawnVolumeSize;
            float randZ = Random.value * spawnVolumeSize;
            particlePos[i] = transform.position + new Vector3(randX, randY, randZ);
        }

        // Create new particles for the particle system.
        particles = new ParticleSystem.Particle[particleCount];
        for (int i = 0; i < particlePos.Length; i++)
        {
            particles[i].position = particlePos[i];
            particles[i].velocity = Vector3.zero;
            particles[i].size = 1.0f;
            particles[i].color = gradient.Evaluate(Random.value);
        }

        // Assign particles back.
        particleSys = gameObject.GetComponent<ParticleSystem>();
        particleSys.SetParticles(particles, particleCount);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
