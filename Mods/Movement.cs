using System;
using GorillaLocomotion;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using StupidTemplate.Classes;
using UnityEngine;
using UnityEngine.XR;
using static StupidTemplate.Menu.Main;

namespace StupidTemplate.Mods
{
    public class Movement
    {
        public static void Fly()
        {
            if (ControllerInputPoller.instance.rightControllerPrimaryButton)
            {
                GTPlayer.Instance.transform.position += GorillaTagger.Instance.headCollider.transform.forward * Time.deltaTime * Settings.Movement.flySpeed;
                GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;
            }
        }

        public static GameObject leftPlatformTop;
        public static GameObject leftPlatformBottom;
        public static GameObject rightPlatformTop;
        public static GameObject rightPlatformBottom;

        public static bool previousLeftGrab;
        public static bool previousRightGrab;

        public static void Platforms()
        {
            bool leftGrab = ControllerInputPoller.instance.leftGrab;
            bool rightGrab = ControllerInputPoller.instance.rightGrab;

            if (leftGrab && !previousLeftGrab)
            {
                if (leftPlatformTop == null && leftPlatformBottom == null)
                {
                    SpawnPlatformPair(true);
                }
                else
                {
                    DestroyPlatformPair(true);
                }
            }

            if (rightGrab && !previousRightGrab)
            {
                if (rightPlatformTop == null && rightPlatformBottom == null)
                {
                    SpawnPlatformPair(false);
                }
                else
                {
                    DestroyPlatformPair(false);
                }
            }

            if (leftPlatformTop != null || leftPlatformBottom != null)
            {
                UpdatePlatformPairPositions(true);
            }

            if (rightPlatformTop != null || rightPlatformBottom != null)
            {
                UpdatePlatformPairPositions(false);
            }

            previousLeftGrab = leftGrab;
            previousRightGrab = rightGrab;
        }

        public static bool previousTeleportTrigger;
        public static void TeleportGun()
        {
            if (ControllerInputPoller.instance.rightGrab)
            {
                var GunData = RenderGun();
                GameObject NewPointer = GunData.NewPointer;

                if (ControllerInputPoller.TriggerFloat(XRNode.RightHand) > 0.5f && !previousTeleportTrigger)
                {
                    GTPlayer.Instance.TeleportTo(NewPointer.transform.position + Vector3.up, GTPlayer.Instance.transform.rotation);
                    GorillaTagger.Instance.rigidbody.linearVelocity = Vector3.zero;
                }

                previousTeleportTrigger = ControllerInputPoller.TriggerFloat(XRNode.RightHand) > 0.5f;
            }
        }

        public static bool previousTagTrigger;
        public static void TagGun()
        {
            if (ControllerInputPoller.instance.rightGrab)
            {
                var gunData = RenderGun();
                VRRig targetRig = gunData.Ray.collider ? gunData.Ray.collider.GetComponentInParent<VRRig>() : null;

                bool triggerPressed = ControllerInputPoller.TriggerFloat(XRNode.RightHand) > 0.5f;
                if (triggerPressed && !previousTagTrigger && targetRig != null && targetRig != VRRig.LocalRig)
                {
                    TryTagRig(targetRig);
                }

                previousTagTrigger = triggerPressed;
            }
            else
            {
                previousTagTrigger = ControllerInputPoller.TriggerFloat(XRNode.RightHand) > 0.5f;
            }
        }

        public static void Speedster()
        {
            Rigidbody playerBody = GorillaTagger.Instance.rigidbody;
            if (playerBody == null)
            {
                return;
            }

            Vector3 velocity = playerBody.velocity;
            if (velocity.sqrMagnitude < 0.01f)
            {
                return;
            }

            const float boostMultiplier = 1.15f;
            const float maxSpeed = 12f;

            Vector3 boostedVelocity = velocity * boostMultiplier;
            if (boostedVelocity.magnitude > maxSpeed)
            {
                boostedVelocity = boostedVelocity.normalized * maxSpeed;
            }

            playerBody.velocity = Vector3.Lerp(velocity, boostedVelocity, Time.deltaTime * 5f);
        }

        private static void SpawnPlatformPair(bool left)
        {
            var (position, rotation, up, _, right) = left ? TrueLeftHand() : TrueRightHand();
            Vector3 offset = up * 0.07f + right * 0.01f;

            GameObject topPlatform = CreatePlatform(position + offset, rotation);
            GameObject bottomPlatform = CreatePlatform(position - offset, rotation);

            if (left)
            {
                leftPlatformTop = topPlatform;
                leftPlatformBottom = bottomPlatform;
            }
            else
            {
                rightPlatformTop = topPlatform;
                rightPlatformBottom = bottomPlatform;
            }
        }

        private static void DestroyPlatformPair(bool left)
        {
            GameObject topPlatform = left ? leftPlatformTop : rightPlatformTop;
            GameObject bottomPlatform = left ? leftPlatformBottom : rightPlatformBottom;

            if (topPlatform != null)
            {
                Object.Destroy(topPlatform);
            }

            if (bottomPlatform != null)
            {
                Object.Destroy(bottomPlatform);
            }

            if (left)
            {
                leftPlatformTop = null;
                leftPlatformBottom = null;
            }
            else
            {
                rightPlatformTop = null;
                rightPlatformBottom = null;
            }
        }

        private static void UpdatePlatformPairPositions(bool left)
        {
            var (position, rotation, up, _, right) = left ? TrueLeftHand() : TrueRightHand();
            Vector3 offset = up * 0.07f + right * 0.01f;

            GameObject topPlatform = left ? leftPlatformTop : rightPlatformTop;
            GameObject bottomPlatform = left ? leftPlatformBottom : rightPlatformBottom;

            if (topPlatform != null)
            {
                topPlatform.transform.SetPositionAndRotation(position + offset, rotation);
            }

            if (bottomPlatform != null)
            {
                bottomPlatform.transform.SetPositionAndRotation(position - offset, rotation);
            }
        }

        private static GameObject CreatePlatform(Vector3 position, Quaternion rotation)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.transform.localScale = new Vector3(0.025f, 0.3f, 0.4f);
            platform.transform.SetPositionAndRotation(position, rotation);

            FixStickyColliders(platform);

            ColorChanger colorChanger = platform.AddComponent<ColorChanger>();
            colorChanger.colors = StupidTemplate.Settings.backgroundColor;

            return platform;
        }

        private static void TryTagRig(VRRig targetRig)
        {
            try
            {
                Player targetPlayer = RigManager.GetPlayerFromVRRig(targetRig);
                GorillaGameManager gameManager = GorillaGameManager.instance;

                if (gameManager != null && targetPlayer != null)
                {
                    var tagMethod = AccessTools.Method(gameManager.GetType(), "TagPlayer");
                    if (tagMethod != null)
                    {
                        tagMethod.Invoke(gameManager, new object[] { targetPlayer, PhotonNetwork.LocalPlayer });
                        return;
                    }
                }

                PhotonView targetView = RigManager.GetPhotonViewFromVRRig(targetRig);
                PhotonView selfView = GorillaTagger.Instance?.photonView;

                if (targetView != null && selfView != null)
                {
                    selfView.RPC("ReportTag", RpcTarget.MasterClient, targetView.ViewID);
                }
            }
            catch (Exception exc)
            {
                Debug.LogError($"{PluginInfo.Name} // Error while trying to tag with Tag Gun: {exc.Message}");
            }
        }
    }
}
