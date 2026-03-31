using UnityEngine;
using System;
using System.Collections.Generic;

public class CharacterController : MonoBehaviour
{
    [SerializeField]
    [Range(0.0f, 1.0f)]
    float walkingSpeed = 1.0f;
    [SerializeField]
    float rotationSpeed = 0.0f;
    [SerializeField]
    bool playOneShot = false;

    [SerializeField]
    AnimationClip actionAnimation;
    [SerializeField]
    AnimationClip idleAnimation;
    [SerializeField]
    AnimationClip walkAnimation;

    AnimationSystem animationSystem;
    Animator animator;
    bool isPlayingOneShotAnim;

    void Start()
    {
        animator = this.GetComponent<Animator>();
        animationSystem = new AnimationSystem(animator, idleAnimation, walkAnimation);
    }

    void Update()
    {
        animationSystem.UpdateLocomotion(new Vector3(0, 0, walkingSpeed), 1);
        if (playOneShot) PlayOneShotAnimation();
    } 

    void FixedUpdate()
    {
        this.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.Self);
    }

    void OnDestroy()
    {
        animationSystem.Destroy();
    }

    void PlayOneShotAnimation()
    {
        if (isPlayingOneShotAnim) return;

        isPlayingOneShotAnim = true;
        animationSystem.PlayOneShot(actionAnimation, () =>
        {
            isPlayingOneShotAnim = false;
            playOneShot = false;
        });
    }
}