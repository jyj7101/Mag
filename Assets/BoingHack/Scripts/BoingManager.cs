using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

namespace BoingHack
{
    using BoingHack.Type;
    using BoingHack.Mono;
    using UnityEngine.Profiling;

    public class BoingManager : MonoSingleton<BoingManager>
    {
        public readonly List<BoingBones> bones = new();
        public readonly Dictionary<int, BoingEffector> effectors = new();

        private static float s_deltaTime = 0.0f;
        public static float DeltaTime => s_deltaTime;

        private int m_lastPumpedFrame = -1;


        private void Update()
        {
            Execute(UpdateMode.EarlyUpdate);
            PullResult(UpdateMode.EarlyUpdate);
            TryPump();
        }

        private void FixedUpdate()
        {
            Execute(UpdateMode.FixedUpdate);
            PullResult(UpdateMode.FixedUpdate);
            TryPump();
        }

        private void LateUpdate()
        {
            Execute(UpdateMode.LateUpdate);
            PullResult(UpdateMode.LateUpdate);
        }

        private void TryPump()
        {
            if (m_lastPumpedFrame >= Time.frameCount)
                return;

            if (m_lastPumpedFrame >= 0)
                DoPump();

            m_lastPumpedFrame = Time.frameCount;
        }

        private void DoPump()
        {
            RestoreBones();
        }



        public static void Register(in BoingReactor reactor)
        {
            switch (reactor)
            {
                case BoingBones bone:
                    Instance.bones.Add(bone);
                    break;

                case BoingEffector effector:
                    Instance.effectors[reactor.GetInstanceID()] = effector;
                    break;
            }
        }

        public static void Unregister(in BoingReactor reactor)
        {
            switch (reactor)
            {
                case BoingBones bone:
                    Instance.bones.Remove(bone);
                    break;

                case BoingEffector effector:
                    Instance.effectors.Remove(effector.GetInstanceID());
                    break;
            }
        }


        internal void Execute(UpdateMode updateMode)
        {
            if (updateMode == UpdateMode.EarlyUpdate)
                s_deltaTime = Time.deltaTime;

            ExecuteBones(updateMode);
        }

        private void PullResult(UpdateMode updateMode)
        {
            PullBonesResults(bones, updateMode);
        }


        internal static void PullBonesResults(List<BoingBones> bonesMap, UpdateMode updateMode)
        {
            Profiler.BeginSample("Update Bones (Pull Results)");

            foreach (var bones in bonesMap)
            {
                if (!bones.isActiveAndEnabled)
                    continue;

                if (!bones.Executed)
                    continue;

                if (bones.UpdateMode != updateMode)
                    continue;

                bones.Params.PullResults(bones);
            }

            Profiler.EndSample();
        }


        private void ExecuteBones(UpdateMode updateMode)
        {
            if (bones.Count == 0)
                return;

            Profiler.BeginSample("BoingManager.ExecuteBones");

            foreach (var bones in bones)
            {
                if (bones.InitRebooted)
                    continue;

                bones.Reboot();
                bones.InitRebooted = true;
            }

            ExecuteBones(bones, updateMode);


            Profiler.EndSample();
        }


        internal static void ExecuteBones(in List<BoingBones> bonesMap, in UpdateMode updateMode)
        {
            Profiler.BeginSample("Update Bones (Execute)");

            float dt = DeltaTime;

            foreach (var bones in bonesMap)
            {
                if (bones.UpdateMode != updateMode)
                    continue;

                bones.PrepareExecute();

                bones.EndAccumulateTargets();
                switch (bones.UpdateMode)
                {
                    case UpdateMode.EarlyUpdate:
                    case UpdateMode.LateUpdate:
                        bones.Params.Execute(bones, DeltaTime);
                        break;

                    case UpdateMode.FixedUpdate:
                        bones.Params.Execute(bones, Time.fixedDeltaTime);
                        break;
                }

                bones.MarkExecuted();
            }

            Profiler.EndSample();
        }


        internal static void ExecuteBones(in List<BoingKit.BoingBones> bonesMap, in UpdateMode updateMode)
        {
            Profiler.BeginSample("Update Bones (Execute)");

            float dt = DeltaTime;

            foreach (var bones in bonesMap)
            {
                if ((int)bones.UpdateMode != (int)updateMode)
                    continue;

                bones.PrepareExecute();

                bones.EndAccumulateTargets();
                switch (bones.UpdateMode)
                {
                    case BoingKit.BoingManager.UpdateMode.EarlyUpdate:
                    case BoingKit.BoingManager.UpdateMode.LateUpdate:
                        bones.Params.Execute(bones, DeltaTime);
                        break;

                    case BoingKit.BoingManager.UpdateMode.FixedUpdate:
                        bones.Params.Execute(bones, Time.fixedDeltaTime);
                        break;
                }

                bones.MarkExecuted();
            }

            Profiler.EndSample();
        }


        private void RestoreBones()
        {
            Profiler.BeginSample("BoingManager.RestoreBones");

            foreach (var bones in bones)
            {
                bones.Restore();
            }

            Profiler.EndSample();
        }
    }


}