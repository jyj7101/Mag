using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using Utils;


namespace BoingHack.Mono
{
    using BoingHack.Type;
    using BoingHack.Utils;

    //Mono
    public class BoingReactor : MonoBehaviour
    {
        public const int ContinuousMotion = 0;
        public UpdateMode updateMode = UpdateMode.LateUpdate;
        public Params Params;

        public bool TwoDDistanceCheck = false;
        public bool TwoDPositionInfluence = false;
        public bool TwoDRotationInfluence = false;
        public bool EnablePositionEffect = true;
        public bool EnableRotationEffect = true;
        public bool EnableScaleEffect = false;
        public bool GlobalReactionUpVector = false;


        private bool m_executed = false;
        public bool Executed => m_executed;
        public void MarkExecuted() { m_executed = true; }

        public BoingReactor()
        {
            Params.Init();
        }

        public void UpdateFlags()
        {
            Params.Bits.SetBit((int)ReactorFlags.TwoDDistanceCheck, TwoDDistanceCheck);
            Params.Bits.SetBit((int)ReactorFlags.TwoDPositionInfluence, TwoDPositionInfluence);
            Params.Bits.SetBit((int)ReactorFlags.TwoDRotationInfluence, TwoDRotationInfluence);
            Params.Bits.SetBit((int)ReactorFlags.EnablePositionEffect, EnablePositionEffect);
            Params.Bits.SetBit((int)ReactorFlags.EnableRotationEffect, EnableRotationEffect);
            Params.Bits.SetBit((int)ReactorFlags.EnableScaleEffect, EnableScaleEffect);
            Params.Bits.SetBit((int)ReactorFlags.GlobalReactionUpVector, GlobalReactionUpVector);

            Params.Bits.SetBit((int)ReactorFlags.FixedUpdate, (updateMode == UpdateMode.FixedUpdate));
            Params.Bits.SetBit((int)ReactorFlags.EarlyUpdate, (updateMode == UpdateMode.EarlyUpdate));
            Params.Bits.SetBit((int)ReactorFlags.LateUpdate, (updateMode == UpdateMode.LateUpdate));
        }

        protected virtual void Register()
        {
            BoingManager.Register(this);
        }

        protected virtual void Unregister()
        {
            BoingManager.Unregister(this);
        }

        public virtual void PrepareExecute()
        {
            UpdateFlags();
        }
    }

}

namespace BoingHack.Type
{

    public enum ParameterMode
    {
        Exponential,
        OscillationByHalfLife,
        OscillationByDampingRatio,
    };

    public enum UpdateMode
    {
        FixedUpdate,
        EarlyUpdate,
        LateUpdate,
    }
    public enum ReactorFlags
    {
        TwoDDistanceCheck,
        TwoDPositionInfluence,
        TwoDRotationInfluence,
        EnablePositionEffect,
        EnableRotationEffect,
        EnableScaleEffect,
        GlobalReactionUpVector,
        EnablePropagation,
        AnchorPropagationAtBorder,
        FixedUpdate,
        EarlyUpdate,
        LateUpdate,
    }

    public enum TwoDPlaneEnum { XY, XZ, YZ };

    public enum TransformLockSpace
    {
        Global,
        Local,
    }
}

namespace BoingHack.Dummy
{
    //Dummy
    public class SharedSphereManager : MonoSingleton<SharedSphereManager>
    {
        //대충싱글톤
        private static SphereCollider _collider;
        public static SphereCollider Collider
        {
            get
            {
                if(_collider == null)
                {
                    _collider = Instance.gameObject.AddComponent<SphereCollider>();
                    _collider.enabled = false;
                }

                return _collider;
            }
        }
    }

}

namespace BoingHack
{
    using BoingHack.Utils;
    using BoingHack.Spring;
    using BoingHack.Mono;
    using BoingHack.Type;
    using BoingHack.Dummy;




    public class BoingBones : BoingReactor
    {

        [Serializable]
        public class Bone
        {
            internal Params.InstanceData Instance;
            internal Transform Transform;
            internal Vector3 ScaleWs;
            internal Vector3 CachedScaleLs;
            internal Vector3 BlendedPositionWs;
            internal Vector3 BlendedScaleLs;
            internal Vector3 CachedPositionWs;
            internal Vector3 CachedPositionLs;
            internal Bounds Bounds;
            internal Quaternion RotationInverseWs;
            internal Quaternion SpringRotationWs;
            internal Quaternion SpringRotationInverseWs;
            internal Quaternion CachedRotationWs;
            internal Quaternion CachedRotationLs;
            internal Quaternion BlendedRotationWs;
            internal Quaternion RotationBackPropDeltaPs;
            internal int ParentIndex;
            internal int[] ChildIndices;
            internal float LengthFromRoot;
            internal float AnimationBlend;
            internal float LengthStiffness;
            internal float LengthStiffnessT;
            internal float FullyStiffToParentLength;
            internal float PoseStiffness;
            internal float BendAngleCap;
            internal float CollisionRadius;
            internal float SquashAndStretch;

            internal void UpdateBounds()
            {
                Bounds = new Bounds(Instance.PositionSpring.Value, 2.0f * CollisionRadius * Vector3.one);
            }

            internal Bone
            (
              Transform transform,
              int iParent,
              float lengthFromRoot
            )
            {
                Transform = transform;
                RotationInverseWs = Quaternion.identity;
                ParentIndex = iParent;
                LengthFromRoot = lengthFromRoot;
                Instance.Reset();
                CachedPositionWs = transform.position;
                CachedPositionLs = transform.localPosition;
                CachedRotationWs = transform.rotation;
                CachedRotationLs = transform.localRotation;
                CachedScaleLs = transform.localScale;
                AnimationBlend = 0.0f;
                LengthStiffness = 0.0f;
                PoseStiffness = 0.0f;
                BendAngleCap = 180.0f;
                CollisionRadius = 0.0f;
            }
        }
        [SerializeField] internal Bone[][] BoneData;

        [Serializable]
        public class Chain
        {
            public enum CurveType
            {
                ConstantOne,
                ConstantHalf,
                ConstantZero,
                RootOneTailHalf,
                RootOneTailZero,
                RootHalfTailOne,
                RootZeroTailOne,
                Custom,
            }

            [Tooltip("Root Transform object from which to build a chain (or tree if a bone has multiple children) of bouncy boing bones.")]
            public Transform Root;

            [Tooltip("List of Transform objects to exclude from chain building.")]
            public Transform[] Exclusion;

            [Tooltip("Enable to allow reaction to boing effectors.")]
            public bool EffectorReaction = true;

            [Tooltip(
                 "Enable to allow root Transform object to be sprung around as well. "
               + "Otherwise, no effects will be applied to the root Transform object."
            )]
            public bool LooseRoot = false;

            [Tooltip(
                 "Assign a SharedParamsOverride asset to override the parameters for this chain. "
               + "Useful for chains using different parameters than that of the BoingBones component."
            )]
            public SharedBoingParams ParamsOverride;

            public CurveType AnimationBlendCurveType = CurveType.RootOneTailZero;
        
            public AnimationCurve AnimationBlendCustomCurve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 0.0f);


            public CurveType LengthStiffnessCurveType = CurveType.ConstantOne;

            public AnimationCurve LengthStiffnessCustomCurve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);


            public CurveType PoseStiffnessCurveType = CurveType.ConstantOne;

            public AnimationCurve PoseStiffnessCustomCurve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);

            public float MaxBendAngleCap = 180.0f;

            public CurveType BendAngleCapCurveType = CurveType.ConstantOne;

            public AnimationCurve BendAngleCapCustomCurve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);


            public float MaxCollisionRadius = 0.1f;

            public CurveType CollisionRadiusCurveType = CurveType.ConstantOne;

            public AnimationCurve CollisionRadiusCustomCurve = AnimationCurve.Linear(0.0f, 1.0f, 1.0f, 1.0f);


            public bool EnableBoingKitCollision = false;


            public bool EnableUnityCollision = false;


            public bool EnableInterChainCollision = false;

            public Vector3 Gravity = Vector3.zero;
            internal Bounds Bounds;


            public CurveType SquashAndStretchCurveType = CurveType.ConstantZero;

            public AnimationCurve SquashAndStretchCustomCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 0.0f);

            public float MaxSquash = 1.1f;

            public float MaxStretch = 2.0f;

            internal Transform m_scannedRoot;
            internal Transform[] m_scannedExclusion;

            internal int m_hierarchyHash = -1;

            internal float MaxLengthFromRoot = 0.0f;

            public static float EvaluateCurve(CurveType type, float t, AnimationCurve curve)
            {
                switch (type)
                {
                    case CurveType.ConstantOne:
                        return 1.0f;

                    case CurveType.ConstantHalf:
                        return 0.5f;

                    case CurveType.ConstantZero:
                        return 0.0f;

                    case CurveType.RootOneTailHalf:
                        return 1.0f - 0.5f * Mathf.Clamp01(t);

                    case CurveType.RootOneTailZero:
                        return 1.0f - Mathf.Clamp01(t);

                    case CurveType.RootHalfTailOne:
                        return 0.5f + 0.5f * Mathf.Clamp01(t);

                    case CurveType.RootZeroTailOne:
                        return Mathf.Clamp01(t);

                    case CurveType.Custom:
                        return curve.Evaluate(t);
                }

                return 0.0f;
            }
        }
        public Chain[] BoneChains = new Chain[1];

        public bool TwistPropagation = true;

        [Range(0.1f, 20.0f)] public float MaxCollisionResolutionSpeed = 3.0f;
        public BoingBoneCollider[] BoingColliders = new BoingBoneCollider[0];
        public Collider[] UnityColliders = new Collider[0];

        public UpdateMode UpdateMode => this.updateMode;

        public TransformLockSpace TranslationLockSpace = TransformLockSpace.Global;
        public bool LockTranslationX = false;
        public bool LockTranslationY = false;
        public bool LockTranslationZ = false;

        public TransformLockSpace RotationLockSpace = TransformLockSpace.Global;
        public bool LockRotationX = false;
        public bool LockRotationY = false;
        public bool LockRotationZ = false;

        public bool InitRebooted = false;

        public bool DebugDrawRawBones = false;
        public bool DebugDrawTargetBones = false;
        public bool DebugDrawBoingBones = false;
        public bool DebugDrawFinalBones = false;
        public bool DebugDrawColliders = false;
        public bool DebugDrawChainBounds = false;
        public bool DebugDrawBoneNames = false;
        public bool DebugDrawLengthFromRoot = false;

        private class RescanEntry
        {
            internal Transform Transform;
            internal int ParentIndex;
            internal float LengthFromRoot;

            internal RescanEntry(Transform transform, int iParent, float lengthFromRoot)
            {
                Transform = transform;
                ParentIndex = iParent;
                LengthFromRoot = lengthFromRoot;
            }
        }

        public void OnValidate()
        {
            RescanBoneChains();
            UpdateCollisionRadius();
        }

        private bool m_justEnabled = false;
        public void OnEnable()
        {
            Register();
            m_justEnabled = true;
        }

        public void OnDisable()
        {
            Unregister();
        }

        public void RescanBoneChains()
        {
            if (BoneChains == null)
                return;

            int numChains = BoneChains.Length;
            if (BoneData == null || BoneData.Length != numChains)
            {
                var newBoneData = new Bone[numChains][];
                if (BoneData != null)
                {
                    for (int iChain = 0, n = Mathf.Min(BoneData.Length, numChains); iChain < n; ++iChain)
                        newBoneData[iChain] = BoneData[iChain];
                }
                BoneData = newBoneData;
            }

            var boneQueue = new Queue<RescanEntry>();
            for (int iChain = 0; iChain < numChains; ++iChain)
            {
                var chain = BoneChains[iChain];

                bool chainNeedsRescan = false;
                if (BoneData[iChain] == null)
                {
                    chainNeedsRescan = true;
                }

                if (!chainNeedsRescan
                    && chain.m_scannedRoot == null)
                {
                    chainNeedsRescan = true;
                }

                if (!chainNeedsRescan
                    && chain.m_scannedRoot != chain.Root)
                {
                    chainNeedsRescan = true;
                }

                if (!chainNeedsRescan
                    && (chain.m_scannedExclusion != null) != (chain.Exclusion != null))
                {
                    chainNeedsRescan = true;
                }

                if (!chainNeedsRescan
                    && chain.Exclusion != null)
                {
                    if (chain.m_scannedExclusion.Length != chain.Exclusion.Length)
                    {
                        chainNeedsRescan = true;
                    }
                    else
                    {
                        for (int i = 0; i < chain.m_scannedExclusion.Length; ++i)
                        {
                            if (chain.m_scannedExclusion[i] == chain.Exclusion[i])
                                continue;

                            chainNeedsRescan = true;
                            break;
                        }
                    }
                }

                var root = chain != null ? chain.Root : null;
                int hierarchyHash = root != null ? Codec.HashTransformHierarchy(root) : -1;

                if (m_justEnabled)
                {
                    chainNeedsRescan = true;
                }
                else
                {
                    if (root != null
                        && chain.m_hierarchyHash != hierarchyHash)
                    {
                        chainNeedsRescan = true;
                    }
                }

                if (!chainNeedsRescan)
                    continue;

                if (root == null)
                {
                    BoneData[iChain] = null;
                    continue;
                }

                chain.m_scannedRoot = chain.Root;
                chain.m_scannedExclusion = chain.Exclusion.ToArray();
                chain.m_hierarchyHash = hierarchyHash;

                chain.MaxLengthFromRoot = 0.0f;

                var aBone = new List<Bone>();
                boneQueue.Enqueue(new RescanEntry(root, -1, 0.0f));
                while (boneQueue.Count > 0)
                {
                    var entry = boneQueue.Dequeue();
                    if (chain.Exclusion.Contains(entry.Transform))
                        continue;

                    int iBone = aBone.Count;

                    var boneTransform = entry.Transform;
                    var aChildIndex = new int[boneTransform.childCount];
                    for (int iChildIndex = 0; iChildIndex < aChildIndex.Length; ++iChildIndex)
                        aChildIndex[iChildIndex] = -1;
                    int numChildrenQueued = 0;
                    for (int iChild = 0, numChildren = boneTransform.childCount; iChild < numChildren; ++iChild)
                    {
                        var childTransform = boneTransform.GetChild(iChild);
                        if (chain.Exclusion.Contains(childTransform))
                            continue;

                        float lengthFromParent = Vector3.Distance(entry.Transform.position, childTransform.position);
                        float lengthFromRoot = entry.LengthFromRoot + lengthFromParent;

                        boneQueue.Enqueue(new RescanEntry(childTransform, iBone, lengthFromRoot));

                        ++numChildrenQueued;
                    }

                    chain.MaxLengthFromRoot = Mathf.Max(entry.LengthFromRoot, chain.MaxLengthFromRoot);

                    var bone = new Bone(boneTransform, entry.ParentIndex, entry.LengthFromRoot);
                    if (numChildrenQueued > 0)
                        bone.ChildIndices = aChildIndex;

                    aBone.Add(bone);
                }

                // fill in child indices
                for (int iBone = 0; iBone < aBone.Count; ++iBone)
                {
                    var bone = aBone[iBone];
                    if (bone.ParentIndex < 0)
                        continue;

                    var parentBone = aBone[bone.ParentIndex];
                    int iChildIndex = 0;
                    while (parentBone.ChildIndices[iChildIndex] >= 0)
                        ++iChildIndex;

                    if (iChildIndex >= parentBone.ChildIndices.Length)
                        continue;

                    parentBone.ChildIndices[iChildIndex] = iBone;
                }

                if (aBone.Count == 0)
                    continue;

                // this is only for getting debug draw in editor mode
                float maxLenFromRootInv = MathUtil.InvSafe(chain.MaxLengthFromRoot);
                for (int iBone = 0; iBone < aBone.Count; ++iBone)
                {
                    var bone = aBone[iBone];
                    float tBone = Mathf.Clamp01(bone.LengthFromRoot * maxLenFromRootInv);
                    bone.CollisionRadius = chain.MaxCollisionRadius * Chain.EvaluateCurve(chain.CollisionRadiusCurveType, tBone, chain.CollisionRadiusCustomCurve);
                }

                BoneData[iChain] = aBone.ToArray();

                Reboot(iChain);
            }
        }

        private void UpdateCollisionRadius()
        {
            for (int iChain = 0; iChain < BoneData.Length; ++iChain)
            {
                var chain = BoneChains[iChain];
                var aBone = BoneData[iChain];

                if (aBone == null)
                    continue;

                // this is only for getting debug draw in editor mode
                float maxLenFromRootInv = MathUtil.InvSafe(chain.MaxLengthFromRoot);
                for (int iBone = 0; iBone < aBone.Length; ++iBone)
                {
                    var bone = aBone[iBone];
                    float tBone = Mathf.Clamp01(bone.LengthFromRoot * maxLenFromRootInv);
                    bone.CollisionRadius = chain.MaxCollisionRadius * Chain.EvaluateCurve(chain.CollisionRadiusCurveType, tBone, chain.CollisionRadiusCustomCurve);
                }
            }
        }

        public void Reboot()
        {
            Params.Instance.PositionSpring.Reset(transform.position);
            Params.Instance.RotationSpring.Reset(transform.rotation);
            Params.Instance.ScaleSpring.Reset(transform.localScale);

            if (BoneData == null)
                RescanBoneChains();

            for (int i = 0; i < BoneData.Length; ++i)
            {
                Reboot(i);
            }
        }

        public void Reboot(int iChain)
        {
            if (BoneData == null)
                RescanBoneChains();

            var aBone = BoneData[iChain];

            if (aBone == null)
                return;

            for (int iBone = 0; iBone < aBone.Length; ++iBone)
            {
                var bone = aBone[iBone];
                bone.Instance.PositionSpring.Reset(bone.Transform.position);
                bone.Instance.RotationSpring.Reset(bone.Transform.rotation);
                bone.CachedPositionWs = bone.Transform.position;
                bone.CachedPositionLs = bone.Transform.localPosition;
                bone.CachedRotationWs = bone.Transform.rotation;
                bone.CachedRotationLs = bone.Transform.localRotation;
                bone.CachedScaleLs = bone.Transform.localScale;
            }
        }

        private float m_minScale = 1.0f;
        internal float MinScale { get { return m_minScale; } }


        public override void PrepareExecute()
        {
            base.PrepareExecute();

            //Params.Instance.PrepareExecute(
            //    ref Params,
            //    CachedPositionWs,
            //    CachedRotationWs,
            //    transform.localScale,
            //    false);


            if (m_justEnabled)
            {
                RescanBoneChains();
                Reboot();
                m_justEnabled = false;
            }

            Params.Bits.SetBit((int)ReactorFlags.EnableRotationEffect, false);

            float fdt = Time.fixedDeltaTime;
            float dt = (updateMode == UpdateMode.FixedUpdate) ? fdt : Time.deltaTime;

            m_minScale = Mathf.Min(transform.localScale.x, transform.localScale.y, transform.localScale.z);

            for (int iChain = 0; iChain < BoneData.Length; ++iChain)
            {
                var chain = BoneChains[iChain];
                var aBone = BoneData[iChain];

                if (aBone == null || chain.Root == null || aBone.Length == 0)
                    continue;

                Vector3 gravityDt = chain.Gravity * dt;

                // update length from root
                float maxLengthFromRoot = 0.0f;
                for (int iBone = 0; iBone < aBone.Length; ++iBone)
                {
                    var bone = aBone[iBone];

                    // root?
                    if (bone.ParentIndex < 0)
                    {
                        if (!chain.LooseRoot)
                        {
                            bone.Instance.PositionSpring.Reset(bone.Transform.position);
                            bone.Instance.RotationSpring.Reset(bone.Transform.rotation);
                        }

                        bone.LengthFromRoot = 0.0f;
                        continue;
                    }

                    var parentBone = aBone[bone.ParentIndex];
                    float distFromParent = Vector3.Distance(bone.Transform.position, parentBone.Transform.position);
                    bone.LengthFromRoot = parentBone.LengthFromRoot + distFromParent;
                    maxLengthFromRoot = Mathf.Max(maxLengthFromRoot, bone.LengthFromRoot);
                }
                float maxLengthFromRootInv = MathUtil.InvSafe(maxLengthFromRoot);

                // set up bones
                for (int iBone = 0; iBone < aBone.Length; ++iBone)
                {
                    var bone = aBone[iBone];

                    // evaluate curves
                    float tBone = bone.LengthFromRoot * maxLengthFromRootInv;
                    bone.AnimationBlend = Chain.EvaluateCurve(chain.AnimationBlendCurveType, tBone, chain.AnimationBlendCustomCurve);
                    bone.LengthStiffness = Chain.EvaluateCurve(chain.LengthStiffnessCurveType, tBone, chain.LengthStiffnessCustomCurve);
                    bone.LengthStiffnessT = 1.0f - Mathf.Pow(1.0f - bone.LengthStiffness, 30.0f * fdt); // a factor of 30.0f is what makes 0.5 length stiffness looks like 50% stiffness
                    bone.FullyStiffToParentLength =
                      bone.ParentIndex >= 0
                      ? Vector3.Distance(aBone[bone.ParentIndex].Transform.position, bone.Transform.position)
                      : 0.0f;
                    bone.PoseStiffness = Chain.EvaluateCurve(chain.PoseStiffnessCurveType, tBone, chain.PoseStiffnessCustomCurve);
                    bone.BendAngleCap = chain.MaxBendAngleCap * MathUtil.Deg2Rad * Chain.EvaluateCurve(chain.BendAngleCapCurveType, tBone, chain.BendAngleCapCustomCurve);
                    bone.CollisionRadius = chain.MaxCollisionRadius * Chain.EvaluateCurve(chain.CollisionRadiusCurveType, tBone, chain.CollisionRadiusCustomCurve);
                    bone.SquashAndStretch = Chain.EvaluateCurve(chain.SquashAndStretchCurveType, tBone, chain.SquashAndStretchCustomCurve);
                }

                var rootBone = aBone[0];
                Vector3 rootAnimPos = rootBone.Transform.position;
                for (int iBone = 0; iBone < aBone.Length; ++iBone)
                {
                    var bone = aBone[iBone];

                    // evaluate curves
                    {
                        float tBone = bone.LengthFromRoot * maxLengthFromRootInv;
                        bone.AnimationBlend = Chain.EvaluateCurve(chain.AnimationBlendCurveType, tBone, chain.AnimationBlendCustomCurve);
                        bone.LengthStiffness = Chain.EvaluateCurve(chain.LengthStiffnessCurveType, tBone, chain.LengthStiffnessCustomCurve);
                        bone.PoseStiffness = Chain.EvaluateCurve(chain.PoseStiffnessCurveType, tBone, chain.PoseStiffnessCustomCurve);
                        bone.BendAngleCap = chain.MaxBendAngleCap * MathUtil.Deg2Rad * Chain.EvaluateCurve(chain.BendAngleCapCurveType, tBone, chain.BendAngleCapCustomCurve);
                        bone.CollisionRadius = chain.MaxCollisionRadius * Chain.EvaluateCurve(chain.CollisionRadiusCurveType, tBone, chain.CollisionRadiusCustomCurve);
                        bone.SquashAndStretch = Chain.EvaluateCurve(chain.SquashAndStretchCurveType, tBone, chain.SquashAndStretchCustomCurve);
                    } // end: evaluate curves

                    // gravity
                    {
                        // no gravity on root
                        if (iBone > 0)
                            bone.Instance.PositionSpring.Velocity += gravityDt;
                    }
                    // end: gravity

                    // compute target transform
                    {
                        bone.RotationInverseWs = Quaternion.Inverse(bone.Transform.rotation);
                        bone.SpringRotationWs = bone.Instance.RotationSpring.ValueQuat;
                        bone.SpringRotationInverseWs = Quaternion.Inverse(bone.SpringRotationWs);

                        Vector3 targetPos = bone.Transform.position;
                        Quaternion targetRot = bone.Transform.rotation;
                        Vector3 targetScale = bone.Transform.localScale;

                        // compute translation & rotation in parent space
                        if (bone.ParentIndex >= 0)
                        {
                            // TODO: use parent spring transform to compute blended position & rotation

                            var parentBone = aBone[bone.ParentIndex];

                            Vector3 parentAnimPos = parentBone.Transform.position;
                            Vector3 parentSpringPos = parentBone.Instance.PositionSpring.Value;

                            Vector3 springPosPs = parentBone.SpringRotationInverseWs * (bone.Instance.PositionSpring.Value - parentSpringPos);
                            Quaternion springRotPs = parentBone.SpringRotationInverseWs * bone.Instance.RotationSpring.ValueQuat;

                            Vector3 animPos = bone.Transform.position;
                            Quaternion animRot = bone.Transform.rotation;
                            Vector3 animPosPs = parentBone.RotationInverseWs * (animPos - parentAnimPos);
                            Quaternion animRotPs = parentBone.RotationInverseWs * animRot;

                            // apply pose stiffness
                            float tPoseStiffness = bone.PoseStiffness;
                            Vector3 blendedPosPs = Vector3.Lerp(springPosPs, animPosPs, tPoseStiffness);
                            Quaternion blendedRotPs = Quaternion.Slerp(springRotPs, animRotPs, tPoseStiffness);

                            targetPos = parentSpringPos + (parentBone.SpringRotationWs * blendedPosPs);
                            targetRot = parentBone.SpringRotationWs * blendedRotPs;

                            // bend angle cap
                            if (bone.BendAngleCap < MathUtil.Pi - MathUtil.Epsilon)
                            {
                                Vector3 targetPosDelta = targetPos - rootAnimPos;
                                targetPosDelta = VectorUtil.ClampBend(targetPosDelta, animPos - rootAnimPos, bone.BendAngleCap);
                                targetPos = rootAnimPos + targetPosDelta;
                            }
                        }

                        if (chain.ParamsOverride == null)
                        {
                            bone.Instance.PrepareExecute
                            (
                              ref Params,
                              targetPos,
                              targetRot,
                              targetScale,
                              true
                            );
                        }
                        else
                        {
                            bone.Instance.PrepareExecute
                            (
                              ref chain.ParamsOverride.Params,
                              targetPos,
                              targetRot,
                              targetScale,
                              true
                            );
                        }
                    } // end: compute target transform
                }
            }
        }

        public void AccumulateTarget(ref BoingEffector.Params effector, float dt)
        {
            for (int iChain = 0; iChain < BoneData.Length; ++iChain)
            {
                var chain = BoneChains[iChain];
                var aBone = BoneData[iChain];

                if (aBone == null)
                    continue;

                if (!chain.EffectorReaction)
                    continue;

                foreach (var bone in aBone)
                {
                    if (chain.ParamsOverride == null)
                    {
                        bone.Instance.AccumulateTarget(ref Params, ref effector, dt);
                    }
                    else
                    {
                        Bits32 bits = chain.ParamsOverride.Params.Bits;
                        chain.ParamsOverride.Params.Bits = Params.Bits;
                        bone.Instance.AccumulateTarget(ref chain.ParamsOverride.Params, ref effector, dt);
                        chain.ParamsOverride.Params.Bits = bits;
                    }
                }
            }
        }

        public void EndAccumulateTargets()
        {
            for (int iChain = 0; iChain < BoneData.Length; ++iChain)
            {
                var chain = BoneChains[iChain];
                var aBone = BoneData[iChain];

                if (aBone == null)
                    continue;

                for (int iBone = 0; iBone < aBone.Length; ++iBone)
                {
                    var bone = aBone[iBone];
                    if (chain.ParamsOverride == null)
                    {
                        bone.Instance.EndAccumulateTargets(ref Params);
                    }
                    else
                    {
                        bone.Instance.EndAccumulateTargets(ref chain.ParamsOverride.Params);
                    }
                }
            }
        }
        public void Restore()
        {
            for (int iChain = 0; iChain < BoneData.Length; ++iChain)
            {
                var chain = BoneChains[iChain];
                var aBone = BoneData[iChain];

                if (aBone == null)
                    continue;

                for (int iBone = 0; iBone < aBone.Length; ++iBone)
                {
                    var bone = aBone[iBone];

                    // skip fixed root
                    if (iBone == 0 && !chain.LooseRoot)
                        continue;

                    bone.Transform.localPosition = bone.CachedPositionLs;
                    bone.Transform.localRotation = bone.CachedRotationLs;
                    bone.Transform.localScale = bone.CachedScaleLs;
                }
            }
        }
    }



    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct Params
    {
        public static readonly int Stride = //   352 bytes
            20 * sizeof(float)              // =  80 bytes
          + 8 * sizeof(int)                // +  32 bytes
          + 1 * InstanceData.Stride;       // + 240 bytes

        public static void Copy(ref Params from, ref Params to)
        {
            to.PositionParameterMode = from.PositionParameterMode;
            to.RotationParameterMode = from.RotationParameterMode;

            to.PositionExponentialHalfLife = from.PositionExponentialHalfLife;
            to.PositionOscillationHalfLife = from.PositionOscillationHalfLife;
            to.PositionOscillationFrequency = from.PositionOscillationFrequency;
            to.PositionOscillationDampingRatio = from.PositionOscillationDampingRatio;
            to.MoveReactionMultiplier = from.MoveReactionMultiplier;
            to.LinearImpulseMultiplier = from.LinearImpulseMultiplier;

            to.RotationExponentialHalfLife = from.RotationExponentialHalfLife;
            to.RotationOscillationHalfLife = from.RotationOscillationHalfLife;
            to.RotationOscillationFrequency = from.RotationOscillationFrequency;
            to.RotationOscillationDampingRatio = from.RotationOscillationDampingRatio;
            to.RotationReactionMultiplier = from.RotationReactionMultiplier;
            to.AngularImpulseMultiplier = from.AngularImpulseMultiplier;

            to.ScaleExponentialHalfLife = from.ScaleExponentialHalfLife;
            to.ScaleOscillationHalfLife = from.ScaleOscillationHalfLife;
            to.ScaleOscillationFrequency = from.ScaleOscillationFrequency;
            to.ScaleOscillationDampingRatio = from.ScaleOscillationDampingRatio;
        }

        // bytes 0-15 (16 bytes)
        public int InstanceID;
        public Bits32 Bits;
        public TwoDPlaneEnum TwoDPlane;
        private int m_padding0;

        // bytes 16-31 (16 bytes)
        public ParameterMode PositionParameterMode;
        public ParameterMode RotationParameterMode;
        public ParameterMode ScaleParameterMode;
        private int m_padding1;

        // bytes 32-95 (64 bytes)
        [Range(0.0f, 5.0f)] public float PositionExponentialHalfLife;
        [Range(0.0f, 5.0f)] public float PositionOscillationHalfLife;
        [Range(0.0f, 10.0f)] public float PositionOscillationFrequency;
        [Range(0.0f, 1.0f)] public float PositionOscillationDampingRatio;
        [Range(0.0f, 10.0f)] public float MoveReactionMultiplier;
        [Range(0.0f, 10.0f)] public float LinearImpulseMultiplier;
        [Range(0.0f, 5.0f)] public float RotationExponentialHalfLife;
        [Range(0.0f, 5.0f)] public float RotationOscillationHalfLife;
        [Range(0.0f, 10.0f)] public float RotationOscillationFrequency;
        [Range(0.0f, 1.0f)] public float RotationOscillationDampingRatio;
        [Range(0.0f, 10.0f)] public float RotationReactionMultiplier;
        [Range(0.0f, 10.0f)] public float AngularImpulseMultiplier;
        [Range(0.0f, 5.0f)] public float ScaleExponentialHalfLife;
        [Range(0.0f, 5.0f)] public float ScaleOscillationHalfLife;
        [Range(0.0f, 10.0f)] public float ScaleOscillationFrequency;
        [Range(0.0f, 1.0f)] public float ScaleOscillationDampingRatio;

        // bytes 96-111 (16 bytes)
        public Vector3 RotationReactionUp;
        private float m_padding2;

        // bytes 112-351 (240 bytes)
        public InstanceData Instance;

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct InstanceData
        {
            public static readonly int Stride = //   240 bytes
              +32 * sizeof(float)              // = 128 bytes
              + 4 * sizeof(int)                // +  16 bytes
              + 2 * Vector3Spring.Stride       // +  64 bytes
              + 1 * QuaternionSpring.Stride;   // +  32 bytes

            // bytes 0-79 (80 bytes)
            public Vector3 PositionTarget;
            private float m_padding0;
            public Vector3 PositionOrigin; // for accumulated target
            private float m_padding1;
            public Vector4 RotationTarget;
            public Vector4 RotationOrigin; // for accumulated target
            public Vector3 ScaleTarget;
            private float m_padding2;

            // bytes 80-95 (16 bytes)
            private int m_numEffectors;
            private int m_instantAccumulation;
            private int m_padding3;
            private int m_padding4;

            // bytes 96-111 (16 bytes)
            private Vector3 m_upWs;
            private float m_minScale;

            // bytes 112-207 (96 bytes)
            public Vector3Spring PositionSpring; // input / output
            public QuaternionSpring RotationSpring; // input / output
            public Vector3Spring ScaleSpring; // input / output

            // bytes 208-239 (32 bytes)
            public Vector3 PositionPropagationWorkData; // temp for propagation
            private float m_padding5;
            public Vector4 RotationPropagationWorkData; // temp for propagation

            public void Reset()
            {
                PositionSpring.Reset();
                RotationSpring.Reset();
                ScaleSpring.Reset(Vector3.one, Vector3.zero);
                PositionPropagationWorkData = Vector3.zero;
                RotationPropagationWorkData = Vector3.zero;
            }

            public void Reset(Vector3 position, bool instantAccumulation)
            {
                PositionSpring.Reset(position);
                RotationSpring.Reset();
                ScaleSpring.Reset(Vector3.one, Vector3.zero);
                PositionPropagationWorkData = Vector3.zero;
                RotationPropagationWorkData = Vector3.zero;
                m_instantAccumulation = instantAccumulation ? 1 : 0;
            }

            // for BoingBehavior & BoingReactor
            public void PrepareExecute(ref Params p, Vector3 position, Quaternion rotation, Vector3 scale, bool accumulateEffectors)
            {
                PositionTarget =
                PositionOrigin = position;

                RotationTarget =
                RotationOrigin = QuaternionUtil.ToVector4(rotation);

                ScaleTarget = scale;

                m_minScale = VectorUtil.MinComponent(scale);

                if (accumulateEffectors)
                {
                    // make relative
                    PositionTarget = Vector3.zero;
                    RotationTarget = Vector4.zero;

                    m_numEffectors = 0;
                    m_upWs =
                      p.Bits.IsBitSet((int)ReactorFlags.GlobalReactionUpVector)
                      ? p.RotationReactionUp
                      : rotation * VectorUtil.NormalizeSafe(p.RotationReactionUp, Vector3.up);
                }
                else
                {
                    m_numEffectors = -1;
                    m_upWs = Vector3.zero;
                }
            }

            // for BoingReactorFied
            public void PrepareExecute(ref Params p, Vector3 gridCenter, Quaternion gridRotation, Vector3 cellOffset)
            {
                PositionOrigin = gridCenter + cellOffset;
                RotationOrigin = QuaternionUtil.ToVector4(Quaternion.identity);

                // make relative
                PositionTarget = Vector3.zero;
                RotationTarget = Vector4.zero;

                m_numEffectors = 0;
                m_upWs =
                  p.Bits.IsBitSet((int)ReactorFlags.GlobalReactionUpVector)
                  ? p.RotationReactionUp
                  : gridRotation * VectorUtil.NormalizeSafe(p.RotationReactionUp, Vector3.up);

                m_minScale = 1.0f;
            }

            public void AccumulateTarget(ref Params p, ref BoingEffector.Params effector, float dt)
            {
                Vector3 effectRefPos =
                  effector.Bits.IsBitSet(BoingReactor.ContinuousMotion)
                    ? VectorUtil.GetClosestPointOnSegment(PositionOrigin, effector.PrevPosition, effector.CurrPosition)
                    : effector.CurrPosition;

                Vector3 deltaPos = PositionOrigin - effectRefPos;

                Vector3 deltaPos3D = deltaPos;
                if (p.Bits.IsBitSet((int)ReactorFlags.TwoDDistanceCheck))
                {
                    switch (p.TwoDPlane)
                    {
                        case TwoDPlaneEnum.XY: deltaPos.z = 0.0f; break;
                        case TwoDPlaneEnum.XZ: deltaPos.y = 0.0f; break;
                        case TwoDPlaneEnum.YZ: deltaPos.x = 0.0f; break;
                    }
                }

                bool inRange =
                     Mathf.Abs(deltaPos.x) <= effector.Radius
                  && Mathf.Abs(deltaPos.y) <= effector.Radius
                  && Mathf.Abs(deltaPos.z) <= effector.Radius
                  && deltaPos.sqrMagnitude <= effector.Radius * effector.Radius;

                if (!inRange)
                    return;

                float deltaDist = deltaPos.magnitude;
                float tDeltaDist =
                  effector.Radius - effector.FullEffectRadius > MathUtil.Epsilon
                  ? 1.0f - Mathf.Clamp01((deltaDist - effector.FullEffectRadius) / (effector.Radius - effector.FullEffectRadius))
                  : 1.0f;

                Vector3 upWsPos = m_upWs;
                Vector3 upWsRot = m_upWs;
                Vector3 deltaDirPos = VectorUtil.NormalizeSafe(deltaPos3D, m_upWs);
                Vector3 deltaDirRot = deltaDirPos;

                if (p.Bits.IsBitSet((int)ReactorFlags.TwoDPositionInfluence))
                {
                    switch (p.TwoDPlane)
                    {
                        case TwoDPlaneEnum.XY: deltaDirPos.z = 0.0f; upWsPos.z = 0.0f; break;
                        case TwoDPlaneEnum.XZ: deltaDirPos.y = 0.0f; upWsPos.y = 0.0f; break;
                        case TwoDPlaneEnum.YZ: deltaDirPos.x = 0.0f; upWsPos.x = 0.0f; break;
                    }

                    if (upWsPos.sqrMagnitude < MathUtil.Epsilon)
                    {
                        switch (p.TwoDPlane)
                        {
                            case TwoDPlaneEnum.XY: upWsPos = Vector3.up; break;
                            case TwoDPlaneEnum.XZ: upWsPos = Vector3.forward; break;
                            case TwoDPlaneEnum.YZ: upWsPos = Vector3.up; break;
                        }
                    }
                    else
                    {
                        upWsPos.Normalize();
                    }

                    deltaDirPos = VectorUtil.NormalizeSafe(deltaDirPos, upWsPos);
                }

                if (p.Bits.IsBitSet((int)ReactorFlags.TwoDRotationInfluence))
                {
                    switch (p.TwoDPlane)
                    {
                        case TwoDPlaneEnum.XY: deltaDirRot.z = 0.0f; upWsRot.z = 0.0f; break;
                        case TwoDPlaneEnum.XZ: deltaDirRot.y = 0.0f; upWsRot.y = 0.0f; break;
                        case TwoDPlaneEnum.YZ: deltaDirRot.x = 0.0f; upWsRot.x = 0.0f; break;
                    }

                    if (upWsRot.sqrMagnitude < MathUtil.Epsilon)
                    {
                        switch (p.TwoDPlane)
                        {
                            case TwoDPlaneEnum.XY: upWsRot = Vector3.up; break;
                            case TwoDPlaneEnum.XZ: upWsRot = Vector3.forward; break;
                            case TwoDPlaneEnum.YZ: upWsRot = Vector3.up; break;
                        }
                    }
                    else
                    {
                        upWsRot.Normalize();
                    }

                    deltaDirRot = VectorUtil.NormalizeSafe(deltaDirRot, upWsRot);
                }

                if (p.Bits.IsBitSet((int)ReactorFlags.EnablePositionEffect))
                {
                    Vector3 moveVec = tDeltaDist * p.MoveReactionMultiplier * effector.MoveDistance * deltaDirPos;
                    PositionTarget += moveVec;

                    PositionSpring.Velocity += tDeltaDist * p.LinearImpulseMultiplier * effector.LinearImpulse * effector.LinearVelocityDir * (60.0f * dt);
                }

                if (p.Bits.IsBitSet((int)ReactorFlags.EnableRotationEffect))
                {
                    Vector3 rotAxis = VectorUtil.NormalizeSafe(Vector3.Cross(upWsRot, deltaDirRot), VectorUtil.FindOrthogonal(upWsRot));
                    Vector3 rotVec = tDeltaDist * p.RotationReactionMultiplier * effector.RotateAngle * rotAxis;
                    RotationTarget += QuaternionUtil.ToVector4(QuaternionUtil.FromAngularVector(rotVec));

                    Vector3 angularImpulseDir = VectorUtil.NormalizeSafe(Vector3.Cross(effector.LinearVelocityDir, deltaDirRot - 0.01f * Vector3.up), rotAxis);
                    float angularImpulseMag = tDeltaDist * p.AngularImpulseMultiplier * effector.AngularImpulse * (60.0f * dt);
                    Vector4 angularImpulseDirQuat = QuaternionUtil.ToVector4(QuaternionUtil.FromAngularVector(angularImpulseDir));
                    RotationSpring.VelocityVec += angularImpulseMag * angularImpulseDirQuat;
                }

                ++m_numEffectors;
            }

            public void EndAccumulateTargets(ref Params p)
            {
                if (m_numEffectors > 0)
                {
                    PositionTarget *= m_minScale / m_numEffectors;
                    PositionTarget += PositionOrigin;

                    RotationTarget /= m_numEffectors;
                    RotationTarget = QuaternionUtil.ToVector4(QuaternionUtil.FromVector4(RotationTarget) * QuaternionUtil.FromVector4(RotationOrigin));
                }
                else
                {
                    PositionTarget = PositionOrigin;
                    RotationTarget = RotationOrigin;
                }
            }

            public void Execute(ref Params p, float dt)
            {
                bool useAccumulatedEffectors = (m_numEffectors >= 0);

                bool positionSpringNeedsUpdate =
                  useAccumulatedEffectors
                  ? (PositionSpring.Velocity.sqrMagnitude > MathUtil.Epsilon
                     || (PositionSpring.Value - PositionTarget).sqrMagnitude > MathUtil.Epsilon)
                  : p.Bits.IsBitSet((int)ReactorFlags.EnablePositionEffect);
                bool rotationSpringNeedsUpdate =
                  useAccumulatedEffectors
                  ? (RotationSpring.VelocityVec.sqrMagnitude > MathUtil.Epsilon
                     || (RotationSpring.ValueVec - RotationTarget).sqrMagnitude > MathUtil.Epsilon)
                  : p.Bits.IsBitSet((int)ReactorFlags.EnableRotationEffect);
                bool scaleSpringNeedsUpdate =
                  p.Bits.IsBitSet((int)ReactorFlags.EnableScaleEffect)
                  && (ScaleSpring.Value - ScaleTarget).sqrMagnitude > MathUtil.Epsilon;

                if (m_numEffectors == 0)
                {
                    bool earlyOut = true;

                    if (positionSpringNeedsUpdate)
                        earlyOut = false;
                    else
                        PositionSpring.Reset(PositionTarget);

                    if (rotationSpringNeedsUpdate)
                        earlyOut = false;
                    else
                        RotationSpring.Reset(QuaternionUtil.FromVector4(RotationTarget));

                    if (earlyOut)
                        return;
                }

                if (m_instantAccumulation != 0)
                {
                    PositionSpring.Value = PositionTarget;
                    RotationSpring.ValueVec = RotationTarget;
                    ScaleSpring.Value = ScaleTarget;
                    m_instantAccumulation = 0;
                }
                else
                {
                    if (positionSpringNeedsUpdate)
                    {
                        switch (p.PositionParameterMode)
                        {
                            case ParameterMode.Exponential:
                                PositionSpring.TrackExponential(PositionTarget, p.PositionExponentialHalfLife, dt);
                                break;

                            case ParameterMode.OscillationByHalfLife:
                                PositionSpring.TrackHalfLife(PositionTarget, p.PositionOscillationFrequency, p.PositionOscillationHalfLife, dt);
                                break;

                            case ParameterMode.OscillationByDampingRatio:
                                PositionSpring.TrackDampingRatio(PositionTarget, p.PositionOscillationFrequency * MathUtil.TwoPi, p.PositionOscillationDampingRatio, dt);
                                break;
                        }
                    }
                    else
                    {
                        PositionSpring.Value = PositionTarget;
                        PositionSpring.Velocity = Vector3.zero;
                    }

                    if (rotationSpringNeedsUpdate)
                    {
                        switch (p.RotationParameterMode)
                        {
                            case ParameterMode.Exponential:
                                RotationSpring.TrackExponential(RotationTarget, p.RotationExponentialHalfLife, dt);
                                break;

                            case ParameterMode.OscillationByHalfLife:
                                RotationSpring.TrackHalfLife(RotationTarget, p.RotationOscillationFrequency, p.RotationOscillationHalfLife, dt);
                                break;

                            case ParameterMode.OscillationByDampingRatio:
                                RotationSpring.TrackDampingRatio(RotationTarget, p.RotationOscillationFrequency * MathUtil.TwoPi, p.RotationOscillationDampingRatio, dt);
                                break;
                        }
                    }
                    else
                    {
                        RotationSpring.ValueVec = RotationTarget;
                        RotationSpring.VelocityVec = Vector4.zero;
                    }

                    if (scaleSpringNeedsUpdate)
                    {
                        switch (p.ScaleParameterMode)
                        {
                            case ParameterMode.Exponential:
                                ScaleSpring.TrackExponential(ScaleTarget, p.ScaleExponentialHalfLife, dt);
                                break;

                            case ParameterMode.OscillationByHalfLife:
                                ScaleSpring.TrackHalfLife(ScaleTarget, p.ScaleOscillationFrequency, p.ScaleOscillationHalfLife, dt);
                                break;

                            case ParameterMode.OscillationByDampingRatio:
                                ScaleSpring.TrackDampingRatio(ScaleTarget, p.ScaleOscillationFrequency * MathUtil.TwoPi, p.ScaleOscillationDampingRatio, dt);
                                break;
                        }
                    }
                    else
                    {
                        ScaleSpring.Value = ScaleTarget;
                        ScaleSpring.Velocity = Vector3.zero;
                    }
                }

                if (!useAccumulatedEffectors)
                {
                    if (!positionSpringNeedsUpdate)
                        PositionSpring.Reset(PositionTarget);
                    if (!rotationSpringNeedsUpdate)
                        RotationSpring.Reset(RotationTarget);
                }
            }

            public void PullResults(BoingBones bones)
            {
                for (int iChain = 0; iChain < bones.BoneData.Length; ++iChain)
                {
                    var chain = bones.BoneChains[iChain];
                    var aBone = bones.BoneData[iChain];

                    if (aBone == null)
                        continue;

                    // must cache before manipulating bone transforms
                    // otherwise, we'd cache delta propagated down from parent bones
                    foreach (var bone in aBone)
                    {
                        bone.CachedPositionWs = bone.Transform.position;
                        bone.CachedPositionLs = bone.Transform.localPosition;
                        bone.CachedRotationWs = bone.Transform.rotation;
                        bone.CachedRotationLs = bone.Transform.localRotation;
                        bone.CachedScaleLs = bone.Transform.localScale;
                    }

                    // blend bone position
                    for (int iBone = 0; iBone < aBone.Length; ++iBone)
                    {
                        var bone = aBone[iBone];

                        // skip fixed root
                        if (iBone == 0 && !chain.LooseRoot)
                        {
                            bone.BlendedPositionWs = bone.CachedPositionWs;
                            continue;
                        }

                        bone.BlendedPositionWs =
                          Vector3.Lerp
                          (
                            bone.Instance.PositionSpring.Value,
                            bone.CachedPositionWs,
                            bone.AnimationBlend
                          );
                    }

                    // rotation delta back-propagation
                    {
                        for (int iBone = 0; iBone < aBone.Length; ++iBone)
                        {
                            var bone = aBone[iBone];

                            // skip fixed root
                            if (iBone == 0 && !chain.LooseRoot)
                            {
                                bone.BlendedRotationWs = bone.CachedRotationWs;
                                continue;
                            }

                            if (bone.ChildIndices == null)
                            {
                                if (bone.ParentIndex >= 0)
                                {
                                    var parentBone = aBone[bone.ParentIndex];
                                    bone.BlendedRotationWs = parentBone.BlendedRotationWs * (parentBone.RotationInverseWs * bone.CachedRotationWs);
                                }

                                continue;
                            }

                            Vector3 bonePos = bone.CachedPositionWs;
                            Vector3 boneBlendedPos = CommonExtension.ComputeTranslationalResults(bone.Transform, bonePos, bone.BlendedPositionWs, bones);
                            Quaternion boneRot = bones.TwistPropagation ? bone.SpringRotationWs : bone.CachedRotationWs;
                            Quaternion boneRotInv = Quaternion.Inverse(boneRot);

                            if (bones.EnableRotationEffect)
                            {
                                Vector4 childRotDeltaPsVecAccumulator = Vector3.zero;
                                float totalWeight = 0.0f;
                                foreach (int iChild in bone.ChildIndices)
                                {
                                    if (iChild < 0)
                                        continue;

                                    var childBone = aBone[iChild];

                                    Vector3 childPos = childBone.CachedPositionWs;
                                    Vector3 childPosDelta = childPos - bonePos;
                                    Vector3 childPosDeltaDir = VectorUtil.NormalizeSafe(childPosDelta, Vector3.zero);

                                    Vector3 childBlendedPos = CommonExtension.ComputeTranslationalResults(childBone.Transform, childPos, childBone.BlendedPositionWs, bones);
                                    Vector3 childBlendedPosDelta = childBlendedPos - boneBlendedPos;
                                    Vector3 childBlendedPosDeltaDir = VectorUtil.NormalizeSafe(childBlendedPosDelta, Vector3.zero);

                                    Quaternion childRotDelta = Quaternion.FromToRotation(childPosDeltaDir, childBlendedPosDeltaDir);
                                    Quaternion childRotDeltaPs = boneRotInv * childRotDelta;

                                    Vector4 childRotDeltaPsVec = QuaternionUtil.ToVector4(childRotDeltaPs);
                                    float weight = Mathf.Max(MathUtil.Epsilon, chain.MaxLengthFromRoot - childBone.LengthFromRoot);
                                    childRotDeltaPsVecAccumulator += weight * childRotDeltaPsVec;
                                    totalWeight += weight;
                                }

                                if (totalWeight > 0.0f)
                                {
                                    Vector4 avgChildRotDeltaPsVec = childRotDeltaPsVecAccumulator / totalWeight;
                                    bone.RotationBackPropDeltaPs = QuaternionUtil.FromVector4(avgChildRotDeltaPsVec);
                                    bone.BlendedRotationWs = (boneRot * bone.RotationBackPropDeltaPs) * boneRot;
                                }
                                else if (bone.ParentIndex >= 0)
                                {
                                    var parentBone = aBone[bone.ParentIndex];
                                    bone.BlendedRotationWs = parentBone.BlendedRotationWs * (parentBone.RotationInverseWs * boneRot);
                                }

                                bone.BlendedRotationWs =
                                  Quaternion.Lerp
                                  (
                                    bone.BlendedRotationWs,
                                    bone.CachedRotationWs,
                                    bone.AnimationBlend
                                  );
                            }
                        }
                    }

                    // write blended position & adjusted rotation into final transforms
                    for (int iBone = 0; iBone < aBone.Length; ++iBone)
                    {
                        var bone = aBone[iBone];

                        // skip fixed root
                        if (iBone == 0 && !chain.LooseRoot)
                        {
                            bone.Instance.PositionSpring.Reset(bone.CachedPositionWs);
                            bone.Instance.RotationSpring.Reset(bone.CachedRotationWs);
                            continue;
                        }

                        bone.Transform.position = CommonExtension.ComputeTranslationalResults(bone.Transform, bone.Transform.position, bone.BlendedPositionWs, bones);
                        bone.Transform.rotation = bone.BlendedRotationWs;
                        bone.Transform.localScale = bone.BlendedScaleLs;
                    }
                }
            }

            private void SuppressWarnings()
            {
                m_padding0 = 0;
                m_padding1 = 0;
                m_padding2 = 0;
                m_padding3 = 0;
                m_padding4 = 0;
                m_padding5 = 0;
                m_padding0 = m_padding1;
                m_padding1 = m_padding2;
                m_padding2 = m_padding3;
                m_padding3 = m_padding4;
                m_padding4 = (int)m_padding0;
                m_padding5 = m_padding0;
            }
        }

        public void Init()
        {
            InstanceID = ~0;

            Bits.Clear();

            TwoDPlane = TwoDPlaneEnum.XZ;

            PositionParameterMode = ParameterMode.OscillationByHalfLife;
            RotationParameterMode = ParameterMode.OscillationByHalfLife;
            ScaleParameterMode = ParameterMode.OscillationByHalfLife;

            PositionExponentialHalfLife = 0.02f;
            PositionOscillationHalfLife = 0.1f;
            PositionOscillationFrequency = 5.0f;
            PositionOscillationDampingRatio = 0.5f;
            MoveReactionMultiplier = 1.0f;
            LinearImpulseMultiplier = 1.0f;

            RotationExponentialHalfLife = 0.02f;
            RotationOscillationHalfLife = 0.1f;
            RotationOscillationFrequency = 5.0f;
            RotationOscillationDampingRatio = 0.5f;
            RotationReactionMultiplier = 1.0f;
            AngularImpulseMultiplier = 1.0f;

            ScaleExponentialHalfLife = 0.02f;
            ScaleOscillationHalfLife = 0.1f;
            ScaleOscillationFrequency = 5.0f;
            ScaleOscillationDampingRatio = 0.5f;

            Instance.Reset();
        }

        public void AccumulateTarget(ref BoingEffector.Params effector, float dt)
        {
            Instance.AccumulateTarget(ref this, ref effector, dt);
        }

        public void EndAccumulateTargets()
        {
            Instance.EndAccumulateTargets(ref this);
        }

        public void Execute(float dt)
        {
            Instance.Execute(ref this, dt);
        }

        public void Execute(BoingBones bones, float dt)
        {
            float maxCollisionResolutionPushLen = bones.MaxCollisionResolutionSpeed * dt;

            for (int iChain = 0; iChain < bones.BoneData.Length; ++iChain)
            {
                var chain = bones.BoneChains[iChain];
                var aBone = bones.BoneData[iChain];

                if (aBone == null)
                    continue;

                // execute boing work
                for (int iBone = 0; iBone < aBone.Length; ++iBone) // skip root
                {
                    var bone = aBone[iBone];

                    if (chain.ParamsOverride == null)
                    {
                        bone.Instance.Execute(ref bones.Params, dt);
                    }
                    else
                    {
                        bone.Instance.Execute(ref chain.ParamsOverride.Params, dt);
                    }
                }

                var rootBone = aBone[0];
                rootBone.ScaleWs = rootBone.BlendedScaleLs = rootBone.CachedScaleLs;

                rootBone.UpdateBounds();
                chain.Bounds = rootBone.Bounds;

                Vector3 rootAnimPos = rootBone.Transform.position;

                // apply length stiffness & volume preservation
                for (int iBone = 1; iBone < aBone.Length; ++iBone) // skip root
                {
                    var bone = aBone[iBone];
                    var parentBone = aBone[bone.ParentIndex];

                    Vector3 toParentVec = parentBone.Instance.PositionSpring.Value - bone.Instance.PositionSpring.Value;
                    Vector3 toParentDir = VectorUtil.NormalizeSafe(toParentVec, Vector3.zero);
                    float toParentLen = toParentVec.magnitude;
                    float fullyStiffLenDelta = toParentLen - bone.FullyStiffToParentLength;
                    float toParentAdjustLen = bone.LengthStiffnessT * fullyStiffLenDelta;

                    // length stiffness
                    {
                        bone.Instance.PositionSpring.Value += toParentAdjustLen * toParentDir;
                        Vector3 velocityInParentAdjustDir = Vector3.Project(bone.Instance.PositionSpring.Velocity, toParentDir);
                        bone.Instance.PositionSpring.Velocity -= bone.LengthStiffnessT * velocityInParentAdjustDir;
                    }

                    // bend angle cap
                    if (bone.BendAngleCap < MathUtil.Pi - MathUtil.Epsilon)
                    {
                        Vector3 animPos = bone.Transform.position;
                        Vector3 posDelta = bone.Instance.PositionSpring.Value - rootAnimPos;
                        posDelta = VectorUtil.ClampBend(posDelta, animPos - rootAnimPos, bone.BendAngleCap);
                        bone.Instance.PositionSpring.Value = rootAnimPos + posDelta;
                    }

                    // volume preservation
                    if (bone.SquashAndStretch > 0.0f)
                    {
                        float toParentLenRatio = toParentLen * MathUtil.InvSafe(bone.FullyStiffToParentLength);
                        float volumePreservationScale = Mathf.Sqrt(1.0f / toParentLenRatio);
                        volumePreservationScale = Mathf.Clamp(volumePreservationScale, 1.0f / Mathf.Max(1.0f, chain.MaxStretch), Mathf.Max(1.0f, chain.MaxSquash));
                        Vector3 volumePreservationScaleVec = VectorUtil.ComponentWiseDivSafe(volumePreservationScale * Vector3.one, parentBone.ScaleWs);

                        bone.BlendedScaleLs =
                          Vector3.Lerp
                          (
                            Vector3.Lerp
                            (
                              bone.CachedScaleLs,
                              volumePreservationScaleVec,
                              bone.SquashAndStretch
                            ),
                            bone.CachedScaleLs,
                            bone.AnimationBlend
                          );
                    }
                    else
                    {
                        bone.BlendedScaleLs = bone.CachedScaleLs;
                    }

                    bone.ScaleWs = VectorUtil.ComponentWiseMult(parentBone.ScaleWs, bone.BlendedScaleLs);

                    bone.UpdateBounds();
                    chain.Bounds.Encapsulate(bone.Bounds);
                }
                chain.Bounds.Expand(0.2f * Vector3.one);

                // Boing Kit colliders
                if (chain.EnableBoingKitCollision)
                {
                    foreach (var collider in bones.BoingColliders)
                    {
                        if (collider == null)
                            continue;

                        if (!chain.Bounds.Intersects(collider.Bounds))
                            continue;

                        foreach (var bone in aBone)
                        {
                            if (!bone.Bounds.Intersects(collider.Bounds))
                                continue;

                            Vector3 push;
                            bool collided = collider.Collide(bone.Instance.PositionSpring.Value, bones.MinScale * bone.CollisionRadius, out push);
                            if (!collided)
                                continue;

                            bone.Instance.PositionSpring.Value += VectorUtil.ClampLength(push, 0.0f, maxCollisionResolutionPushLen);
                            bone.Instance.PositionSpring.Velocity -= Vector3.Project(bone.Instance.PositionSpring.Velocity, push);
                        }
                    }
                }

                // Unity colliders
                var sharedSphereCollider = SharedSphereManager.Collider;
                if (chain.EnableUnityCollision && sharedSphereCollider != null)
                {
                    sharedSphereCollider.enabled = true;

                    foreach (var collider in bones.UnityColliders)
                    {
                        if (collider == null)
                            continue;

                        if (!chain.Bounds.Intersects(collider.bounds))
                            continue;

                        foreach (var bone in aBone)
                        {
                            if (!bone.Bounds.Intersects(collider.bounds))
                                continue;

                            sharedSphereCollider.center = bone.Instance.PositionSpring.Value;
                            sharedSphereCollider.radius = bone.CollisionRadius;

                            Vector3 pushDir;
                            float pushDist;
                            bool collided =
                              Physics.ComputePenetration
                              (
                                sharedSphereCollider, Vector3.zero, Quaternion.identity,
                                collider, collider.transform.position, collider.transform.rotation,
                                out pushDir, out pushDist
                              );
                            if (!collided)
                                continue;

                            bone.Instance.PositionSpring.Value += VectorUtil.ClampLength(pushDir * pushDist, 0.0f, maxCollisionResolutionPushLen);
                            bone.Instance.PositionSpring.Velocity -= Vector3.Project(bone.Instance.PositionSpring.Velocity, pushDir);
                        }
                    }

                    sharedSphereCollider.enabled = false;
                }

                // self collision
                if (chain.EnableInterChainCollision)
                {
                    foreach (var bone in aBone)
                    {
                        for (int iOtherChain = iChain + 1; iOtherChain < bones.BoneData.Length; ++iOtherChain)
                        {
                            var otherChain = bones.BoneChains[iOtherChain];
                            var aOtherBone = bones.BoneData[iOtherChain];

                            if (aOtherBone == null)
                                continue;

                            if (!otherChain.EnableInterChainCollision)
                                continue;

                            if (!chain.Bounds.Intersects(otherChain.Bounds))
                                continue;

                            foreach (var otherBone in aOtherBone)
                            {
                                Vector3 push;
                                bool collided =
                                  Collision.SphereSphere
                                  (
                                    bone.Instance.PositionSpring.Value,
                                    bones.MinScale * bone.CollisionRadius,
                                    otherBone.Instance.PositionSpring.Value,
                                    bones.MinScale * otherBone.CollisionRadius,
                                    out push
                                  );
                                if (!collided)
                                    continue;

                                push = VectorUtil.ClampLength(push, 0.0f, maxCollisionResolutionPushLen);

                                float pushRatio = otherBone.CollisionRadius * MathUtil.InvSafe(bone.CollisionRadius + otherBone.CollisionRadius);
                                bone.Instance.PositionSpring.Value += pushRatio * push;
                                otherBone.Instance.PositionSpring.Value -= (1.0f - pushRatio) * push;

                                bone.Instance.PositionSpring.Velocity -= Vector3.Project(bone.Instance.PositionSpring.Velocity, push);
                                otherBone.Instance.PositionSpring.Velocity -= Vector3.Project(otherBone.Instance.PositionSpring.Velocity, push);
                            }
                        }
                    }
                }
            } // end: foreach bone chain
        }

        public void PullResults(BoingBones bones)
        {
            Instance.PullResults(bones);
        }

        private void SuppressWarnings()
        {
            m_padding0 = 0;
            m_padding1 = 0;
            m_padding2 = 0.0f;
            m_padding0 = m_padding1;
            m_padding1 = m_padding0;
            m_padding2 = m_padding0;
        }
    }

}