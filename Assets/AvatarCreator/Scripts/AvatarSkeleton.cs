using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

static public class TransformExtension
{
    static public void SetPosX(this Transform trans, float x)
    {
        Vector3 pos = trans.position;
        pos.x = x;
        trans.position = pos;
    }

    static public void SetPosY(this Transform trans, float y)
    {
        Vector3 pos = trans.position;
        pos.y = y;
        trans.position = pos;
    }

    static public void SetPosZ(this Transform trans, float z)
    {
        Vector3 pos = trans.position;
        pos.z = z;
        trans.position = pos;
    }
}

public class AvatarSkeleton : MonoBehaviour
{
    public enum BoneNameConvention
    {
        Motive,
        FBX,
        BVH,
    }

    #region ### Veriables ###
    private Dictionary<string, string> _cachedMecanimBoneNameMap = new Dictionary<string, string>();
    private string _assetName = "TestSkeleton";

    private Avatar _srcAvatar;
    private bool _initialized = false;

    private Avatar _destAvatar;

    [SerializeField]
    [Tooltip("肩から肘への距離係数")]
    private float _elbowDistanceCoff = 0.28f;

    [SerializeField]
    [Tooltip("肩からArmまでの距離係数")]
    private float _armDistanceCoff = 0.05f;

    [SerializeField]
    [Tooltip("首元から肩へのX位置オフセット")]
    private float _shoulderOffsetX = 0.1f;

    [SerializeField]
    [Tooltip("首元から肩へのY位置オフセット")]
    private float _shoulderOffsetY = 0.05f;

    [SerializeField]
    [Tooltip("Headの位置から肩への位置オフセット")]
    private float _nectOffset = 0.17f;

    [SerializeField]
    [Tooltip("HeadからHipへのオフセット")]
    private float _hipOffset = 0.8f;

    [SerializeField]
    [Tooltip("HipからのUpperLeg左右オフセット")]
    private float _upperLegHorizontalOffset = 0.1f;

    [SerializeField]
    [Tooltip("HipからのUpperLeg上下オフセット")]
    private float _upperLegVerticalOffset = 0.1f;

    [SerializeField]
    [Tooltip("HipからFootにかけての距離から膝の位置を算出する係数")]
    private float _lowerLegDistanceCoff = 0.5f;

    [SerializeField]
    private Color[] _colors = new Color[20];

    [SerializeField]
    private Material _lineMaterial;

    #region ### ボーンへの参照 ###
    [SerializeField]
    private Transform _root;

    [SerializeField]
    private Transform _head;

    [SerializeField]
    private Transform _neck;

    [SerializeField]
    private Transform _hips;

    [SerializeField]
    private Transform _spine;

    [SerializeField]
    private Transform _chest;

    [SerializeField]
    private Transform _leftShoulder;

    [SerializeField]
    private Transform _leftUpperArm;

    [SerializeField]
    private Transform _leftLowerArm;

    [SerializeField]
    private Transform _leftHand;

    [SerializeField]
    private Transform _rightShoulder;

    [SerializeField]
    private Transform _rightUpperArm;

    [SerializeField]
    private Transform _rightLowerArm;

    [SerializeField]
    private Transform _rightHand;

    [SerializeField]
    private Transform _leftUpperLeg;

    [SerializeField]
    private Transform _leftLowerLeg;

    [SerializeField]
    private Transform _leftFoot;

    [SerializeField]
    private Transform _leftToes;

    [SerializeField]
    private Transform _rightUpperLeg;

    [SerializeField]
    private Transform _rightLowerLeg;

    [SerializeField]
    private Transform _rightFoot;

    [SerializeField]
    private Transform _rightToes;
    #endregion ### ボーンへの参照 ###

    private Dictionary<string, Transform> _transformDefinision = new Dictionary<string, Transform>();
    private List<Transform> _skeletonBones = new List<Transform>();

    private HumanPoseHandler _srchandler;
    private List<Animator> _targetAnimators = new List<Animator>();
    private Dictionary<Animator, HumanPoseHandler> _destHandlerDict = new Dictionary<Animator, HumanPoseHandler>();
    private HumanPose _humanPose = new HumanPose();
    #endregion ### Veriables ###

    #region ### デバッグ ###
    [SerializeField]
    private bool _showVisualizer = true;

    [SerializeField]
    private GameObject[] _visualizerObjects;

    private List<LineRenderer> _lineRenderers = new List<LineRenderer>();
    #endregion ### デバッグ ###

    #region ### MonoBehaviour ###
    private void Start()
    {
        CacheBoneNameMap(BoneNameConvention.FBX, _assetName);
        SetupSkeleton();
        SetupBones();

        SetupLineRenderers();
    }

    private void Update()
    {
        UpdateLineRenderers();

        if (!_initialized)
        {
            return;
        }

        UpdatePose();
    }

    private void OnValidate()
    {
        foreach (var obj in _visualizerObjects)
        {
            obj.SetActive(_showVisualizer);
        }

        foreach (var ren in _lineRenderers)
        {
            ren.gameObject.SetActive(_showVisualizer);
        }
    }

    private void OnDrawGizmos()
    {
        if (!_showVisualizer)
        {
            return;
        }

        Gizmos.color = Color.red;

        Gizmos.DrawLine(_head.position, _neck.position);
        Gizmos.DrawLine(_neck.position, _spine.position);
        Gizmos.DrawLine(_neck.position, _leftShoulder.position);
        Gizmos.DrawLine(_neck.position, _rightShoulder.position);
        Gizmos.DrawLine(_chest.position, _spine.position);
        Gizmos.DrawLine(_spine.position, _hips.position);

        Gizmos.DrawLine(_hips.position, _leftUpperLeg.position);
        Gizmos.DrawLine(_hips.position, _rightUpperLeg.position);

        Gizmos.DrawLine(_leftShoulder.position, _leftUpperArm.position);
        Gizmos.DrawLine(_leftUpperArm.position, _leftLowerArm.position);
        Gizmos.DrawLine(_leftLowerArm.position, _leftHand.position);

        Gizmos.DrawLine(_rightShoulder.position, _rightUpperArm.position);
        Gizmos.DrawLine(_rightUpperArm.position, _rightLowerArm.position);
        Gizmos.DrawLine(_rightLowerArm.position, _rightHand.position);

        Gizmos.DrawLine(_leftUpperLeg.position, _leftLowerLeg.position);
        Gizmos.DrawLine(_leftLowerLeg.position, _leftFoot.position);
        Gizmos.DrawLine(_leftFoot.position, _leftToes.position);

        Gizmos.DrawLine(_rightUpperLeg.position, _rightLowerLeg.position);
        Gizmos.DrawLine(_rightLowerLeg.position, _rightFoot.position);
        Gizmos.DrawLine(_rightFoot.position, _rightToes.position);
    }
    #endregion ### MonoBehaviour ###

    /// <summary>
    /// デバッグ用に、LineRendereをセットアップ
    /// </summary>
    private void SetupLineRenderers()
    {
        GameObject rendererRoot = new GameObject("RendererRoot");
        rendererRoot.transform.SetParent(transform, false);
        rendererRoot.transform.localPosition = Vector3.zero;
        rendererRoot.transform.localRotation = Quaternion.identity;

        for (int i = 0; i < 20; i++)
        {
            GameObject renderer = new GameObject("renderer", typeof(LineRenderer));
            renderer.transform.SetParent(rendererRoot.transform, false);
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localRotation = Quaternion.identity;
            LineRenderer line = renderer.GetComponent<LineRenderer>();
            line.material = new Material(_lineMaterial);
            line.material.color = _colors[i];
            line.startWidth = 0.01f;
            line.endWidth = 0.01f;
            _lineRenderers.Add(line);
        }
    }

    /// <summary>
    /// LineRenderの位置をアップデート
    /// </summary>
    private void UpdateLineRenderers()
    {
        Vector3[] positions = new[]
        {
            _head.position, _neck.position,
            _neck.position, _spine.position,
            _neck.position, _leftShoulder.position,
            _neck.position, _rightShoulder.position,
            _chest.position, _spine.position,
            _spine.position, _hips.position,
            _hips.position, _leftUpperLeg.position,
            _hips.position, _rightUpperLeg.position,
            _leftShoulder.position, _leftUpperArm.position,
            _leftUpperArm.position, _leftLowerArm.position,
            _leftLowerArm.position, _leftHand.position,
            _rightShoulder.position, _rightUpperArm.position,
            _rightUpperArm.position, _rightLowerArm.position,
            _rightLowerArm.position, _rightHand.position,
            _leftUpperLeg.position, _leftLowerLeg.position,
            _leftLowerLeg.position, _leftFoot.position,
            _leftFoot.position, _leftToes.position,
            _rightUpperLeg.position, _rightLowerLeg.position,
            _rightLowerLeg.position, _rightFoot.position,
            _rightFoot.position, _rightToes.position,
        };

        for (int i = 0; i < positions.Length; i += 2)
        {
            _lineRenderers[i / 2].SetPosition(0, positions[i + 0]);
            _lineRenderers[i / 2].SetPosition(1, positions[i + 1]);
        }
    }

    /// <summary>
    /// ポーズを更新し、登録されている出力アバターにも同期する
    /// </summary>
    private void UpdatePose()
    {
        if (_srchandler != null && _destHandlerDict.Count > 0)
        {
            _srchandler.GetHumanPose(ref _humanPose);

            foreach (var handler in _destHandlerDict.Values)
            {
                handler.SetHumanPose(ref _humanPose);
            }
        }
    }

    /// <summary>
    /// アバターを生成し、設定された対象アニメータにアバターを同期させる
    /// </summary>
    public void Create()
    {
        Setup();
    }

    /// <summary>
    /// 基礎となるボーン構造をキャリブレーションする
    /// </summary>
    public void Calibration(Transform headTrans, Transform leftHandTrans, Transform rightHandTrans, Transform leftFootTrans = null, Transform rightFootTrans = null)
    {
        #region ### 上半身のキャリブレーション ###
        // まず、頭の位置を中心として、スケルトンボーンの中心位置をX座標に合わせる
        transform.SetPosX(headTrans.position.x);
        transform.SetPosZ(headTrans.position.z);

        Vector3 headPos = headTrans.position;
        headPos.z = _head.position.z;

        // 腰の位置を設定
        Vector3 hipPos = headPos + (Vector3.down * _hipOffset);
        hipPos.z = _hips.position.z;
        _hips.position = hipPos;

        // 首の高さから肩の高さを算出
        Vector3 necOffset = Vector3.down * _nectOffset;

        // 首の位置を計算
        Vector3 neckPos = headPos + necOffset;
        neckPos.z = _neck.position.z;
        _neck.position = neckPos;

        // 続いて、頭のY位置を同期する
        _head.position = headPos;

        // 左手の位置
        Vector3 leftHandPos = leftHandTrans.position;
        leftHandPos.z = _leftHand.position.z;

        // 右手の位置
        Vector3 rightHandPos = rightHandTrans.position;
        rightHandPos.z = _rightHand.position.z;

        // 肩の高さ位置
        Vector3 shoulderYPos = neckPos + (Vector3.down * _shoulderOffsetY);

        // 左肩の位置計算
        Vector3 leftHandPosProj = leftHandPos;
        leftHandPosProj.y = shoulderYPos.y;
        Vector3 leftShoulderDir = (leftHandPosProj - shoulderYPos).normalized;
        Vector3 leftShoulderPos = shoulderYPos + (leftShoulderDir * _shoulderOffsetX);
        leftShoulderPos.z = _leftShoulder.position.z;
        _leftShoulder.position = leftShoulderPos;

        // 右肩の位置計算
        Vector3 rightHandPosProj = rightHandPos;
        rightHandPosProj.y = shoulderYPos.y;
        Vector3 rightShoulderDir = (rightHandPosProj - shoulderYPos).normalized;
        Vector3 rightShoulderPos = shoulderYPos + (rightShoulderDir * _shoulderOffsetX);
        rightShoulderPos.z = _rightShoulder.position.z;
        _rightShoulder.position = rightShoulderPos;

        Vector3 leftArmDir = (leftHandPos - _leftShoulder.position).normalized;
        Vector3 leftArmPos = _leftShoulder.position + (leftArmDir * _armDistanceCoff);
        leftArmPos.z = _leftUpperArm.position.z;
        _leftUpperArm.position = leftArmPos;

        Vector3 rightArmDir = (rightHandPos - _rightShoulder.position).normalized;
        Vector3 rightArmPos = _rightShoulder.position + (rightArmDir * _armDistanceCoff);
        rightArmPos.z = _rightUpperArm.position.z;
        _rightUpperArm.position = rightArmPos;

        Vector3 leftElbowPos = _leftShoulder.position + ((leftHandPos - _leftShoulder.position) * _elbowDistanceCoff);
        leftElbowPos.z = _leftLowerArm.position.z;
        _leftLowerArm.position = leftElbowPos;

        Vector3 rightElbowPos = _rightShoulder.position + ((rightHandPos - _rightShoulder.position) * _elbowDistanceCoff);
        rightElbowPos.z = _rightLowerArm.position.z;
        _rightLowerArm.position = rightElbowPos;

        // 手の位置を、Z軸以外を同期する
        _leftHand.position = leftHandPos;
        _rightHand.position = rightHandPos;
        #endregion ### 上半身のキャリブレーション ###

        // どちらか片方でもnullだった場合はセットアップしない
        if (leftFootTrans == null || rightFootTrans == null)
        {
            return;
        }

        #region ### 下半身のキャリブレーション ###
        Vector3 leftFootPos = leftFootTrans.position;
        leftFootPos.z = _leftFoot.position.z;

        Vector3 rightFootPos = rightFootTrans.position;
        rightFootPos.z = _rightFoot.position.z;

        Vector3 leftUpperLegTarget = leftFootPos;
        leftUpperLegTarget.y = hipPos.y;
        Vector3 leftLegDir = (leftUpperLegTarget - hipPos).normalized;

        Vector3 leftUpperLegPos = hipPos + (leftLegDir * _upperLegHorizontalOffset) + (Vector3.down * _upperLegVerticalOffset);
        _leftUpperLeg.position = leftUpperLegPos;

        Vector3 rightUpperLegTarget = rightFootPos;
        rightUpperLegTarget.y = hipPos.y;
        Vector3 rightLegDir = (rightUpperLegTarget - hipPos).normalized;

        Vector3 rightUpperLegPos = hipPos + (rightLegDir * _upperLegHorizontalOffset) + (Vector3.down * _upperLegVerticalOffset);
        _rightUpperLeg.position = rightUpperLegPos;

        Vector3 leftLowerPos = (leftUpperLegPos - leftFootPos) * _lowerLegDistanceCoff;
        _leftLowerLeg.position = leftFootPos + leftLowerPos;

        Vector3 rightLowerPos = (rightUpperLegPos - rightFootPos) * _lowerLegDistanceCoff;
        _rightLowerLeg.position = rightFootPos + rightLowerPos;

        _leftToes.position = leftFootPos;
        _rightToes.position = rightFootPos;
        #endregion ### LegBonesのキャリブレーション ###
    }

    /// <summary>
    /// アサインされたTransformからボーンのリストをセットアップする
    /// </summary>
    private void SetupBones()
    {
        _transformDefinision.Clear();

        _transformDefinision.Add("Hips", _hips);
        _transformDefinision.Add("Spine", _spine);
        _transformDefinision.Add("Chest", _chest);
        _transformDefinision.Add("Neck", _neck);
        _transformDefinision.Add("Head", _head);
        _transformDefinision.Add("LeftShoulder", _leftShoulder);
        _transformDefinision.Add("LeftUpperArm", _leftUpperArm);
        _transformDefinision.Add("LeftLowerArm", _leftLowerArm);
        _transformDefinision.Add("LeftHand", _leftHand);
        _transformDefinision.Add("RightShoulder", _rightShoulder);
        _transformDefinision.Add("RightUpperArm", _rightUpperArm);
        _transformDefinision.Add("RightLowerArm", _rightLowerArm);
        _transformDefinision.Add("RightHand", _rightHand);
        _transformDefinision.Add("LeftUpperLeg", _leftUpperLeg);
        _transformDefinision.Add("LeftLowerLeg", _leftLowerLeg);
        _transformDefinision.Add("LeftFoot", _leftFoot);
        _transformDefinision.Add("RightUpperLeg", _rightUpperLeg);
        _transformDefinision.Add("RightLowerLeg", _rightLowerLeg);
        _transformDefinision.Add("RightFoot", _rightFoot);
        _transformDefinision.Add("LeftToes", _leftToes);
        _transformDefinision.Add("RightToes", _rightToes);
    }

    /// <summary>
    /// 再帰的にボーン構造走査して構成を把握する
    /// </summary>
    private void SetupSkeleton()
    {
        _skeletonBones.Clear();
        RecursiveSkeleton(_root, ref _skeletonBones);
    }

    /// <summary>
    /// 再帰的にTransformを走査して、ボーン構造を生成する
    /// </summary>
    /// <param name="current">現在のTransform</param>
    private void RecursiveSkeleton(Transform current, ref List<Transform> skeletons)
    {
        skeletons.Add(current);

        for (int i = 0; i < current.childCount; i++)
        {
            Transform child = current.GetChild(i);
            RecursiveSkeleton(child, ref skeletons);
        }
    }

    /// <summary>
    /// アバターのセットアップ
    /// </summary>
    private void Setup()
    {
        string[] humanTraitBoneNames = HumanTrait.BoneName;

        List<HumanBone> humanBones = new List<HumanBone>(humanTraitBoneNames.Length);
        for (int i = 0; i < humanTraitBoneNames.Length; i++)
        {
            string humanBoneName = humanTraitBoneNames[i];
            Transform bone;
            if (_transformDefinision.TryGetValue(humanBoneName, out bone))
            {
                HumanBone humanBone = new HumanBone();
                humanBone.humanName = humanBoneName;
                humanBone.boneName = bone.name;
                humanBone.limit.useDefaultValues = true;

                humanBones.Add(humanBone);
            }
        }

        List<SkeletonBone> skeletonBones = new List<SkeletonBone>(_skeletonBones.Count + 1);

        for (int i = 0; i < _skeletonBones.Count; i++)
        {
            Transform bone = _skeletonBones[i];

            SkeletonBone skelBone = new SkeletonBone();
            skelBone.name = bone.name;
            skelBone.position = bone.localPosition;
            skelBone.rotation = bone.localRotation;
            skelBone.scale = Vector3.one;

            skeletonBones.Add(skelBone);
        }

        HumanDescription humanDesc = new HumanDescription();
        humanDesc.human = humanBones.ToArray();
        humanDesc.skeleton = skeletonBones.ToArray();

        humanDesc.upperArmTwist = 0.5f;
        humanDesc.lowerArmTwist = 0.5f;
        humanDesc.upperLegTwist = 0.5f;
        humanDesc.lowerLegTwist = 0.5f;
        humanDesc.armStretch = 0.05f;
        humanDesc.legStretch = 0.05f;
        humanDesc.feetSpacing = 0.0f;
        humanDesc.hasTranslationDoF = false;

        _srcAvatar = AvatarBuilder.BuildHumanAvatar(gameObject, humanDesc);
        _srcAvatar.name = "AvatarSystem";

        if (!_srcAvatar.isValid || !_srcAvatar.isHuman)
        {
            Debug.LogError("setup error");
            return;
        }

        _srchandler = new HumanPoseHandler(_srcAvatar, transform);

        _initialized = true;
    }

    /// <summary>
    /// 生成したアバターのターゲットを登録する
    /// </summary>
    /// <param name="target"></param>
    public void AddTarget(Animator target)
    {
        if (!_initialized)
        {
            Debug.Log("Must initialize avatar skeleton.");
            return;
        }

        if (_targetAnimators.Contains(target))
        {
            return;
        }

        _targetAnimators.Add(target);
        Avatar destAvatar = target.avatar;
        HumanPoseHandler destHandler = new HumanPoseHandler(destAvatar, target.transform);
        _destHandlerDict.Add(target, destHandler);
    }

    /// <summary>
    /// 登録されているアバターを削除する
    /// </summary>
    /// <param name="target">削除対象のアニメータ</param>
    public void RemoveTarget(Animator target)
    {
        if (!_targetAnimators.Contains(target))
        {
            return;
        }

        _targetAnimators.Remove(target);
        _destHandlerDict.Remove(target);
    }

    /// <summary>
    /// Updates the <see cref="_cachedMecanimBoneNameMap"/> lookup to reflect the specified bone naming convention
    /// and source skeleton asset name.
    /// </summary>
    /// <param name="convention">The bone naming convention to use. Must match the host software.</param>
    /// <param name="assetName">The name of the source skeleton asset.</param>
    private void CacheBoneNameMap(BoneNameConvention convention, string assetName)
    {
        _cachedMecanimBoneNameMap.Clear();

        switch (convention)
        {
            case BoneNameConvention.Motive:
                _cachedMecanimBoneNameMap.Add("Hips", assetName + "_Hip");
                _cachedMecanimBoneNameMap.Add("Spine", assetName + "_Ab");
                _cachedMecanimBoneNameMap.Add("Chest", assetName + "_Chest");
                _cachedMecanimBoneNameMap.Add("Neck", assetName + "_Neck");
                _cachedMecanimBoneNameMap.Add("Head", assetName + "_Head");
                _cachedMecanimBoneNameMap.Add("LeftShoulder", assetName + "_LShoulder");
                _cachedMecanimBoneNameMap.Add("LeftUpperArm", assetName + "_LUArm");
                _cachedMecanimBoneNameMap.Add("LeftLowerArm", assetName + "_LFArm");
                _cachedMecanimBoneNameMap.Add("LeftHand", assetName + "_LHand");
                _cachedMecanimBoneNameMap.Add("RightShoulder", assetName + "_RShoulder");
                _cachedMecanimBoneNameMap.Add("RightUpperArm", assetName + "_RUArm");
                _cachedMecanimBoneNameMap.Add("RightLowerArm", assetName + "_RFArm");
                _cachedMecanimBoneNameMap.Add("RightHand", assetName + "_RHand");
                _cachedMecanimBoneNameMap.Add("LeftUpperLeg", assetName + "_LThigh");
                _cachedMecanimBoneNameMap.Add("LeftLowerLeg", assetName + "_LShin");
                _cachedMecanimBoneNameMap.Add("LeftFoot", assetName + "_LFoot");
                _cachedMecanimBoneNameMap.Add("RightUpperLeg", assetName + "_RThigh");
                _cachedMecanimBoneNameMap.Add("RightLowerLeg", assetName + "_RShin");
                _cachedMecanimBoneNameMap.Add("RightFoot", assetName + "_RFoot");
                _cachedMecanimBoneNameMap.Add("LeftToeBase", assetName + "_LToe");
                _cachedMecanimBoneNameMap.Add("RightToeBase", assetName + "_RToe");

                _cachedMecanimBoneNameMap.Add("Left Thumb Proximal", assetName + "_LThumb1");
                _cachedMecanimBoneNameMap.Add("Left Thumb Intermediate", assetName + "_LThumb2");
                _cachedMecanimBoneNameMap.Add("Left Thumb Distal", assetName + "_LThumb3");
                _cachedMecanimBoneNameMap.Add("Right Thumb Proximal", assetName + "_RThumb1");
                _cachedMecanimBoneNameMap.Add("Right Thumb Intermediate", assetName + "_RThumb2");
                _cachedMecanimBoneNameMap.Add("Right Thumb Distal", assetName + "_RThumb3");

                _cachedMecanimBoneNameMap.Add("Left Index Proximal", assetName + "_LIndex1");
                _cachedMecanimBoneNameMap.Add("Left Index Intermediate", assetName + "_LIndex2");
                _cachedMecanimBoneNameMap.Add("Left Index Distal", assetName + "_LIndex3");
                _cachedMecanimBoneNameMap.Add("Right Index Proximal", assetName + "_RIndex1");
                _cachedMecanimBoneNameMap.Add("Right Index Intermediate", assetName + "_RIndex2");
                _cachedMecanimBoneNameMap.Add("Right Index Distal", assetName + "_RIndex3");

                _cachedMecanimBoneNameMap.Add("Left Middle Proximal", assetName + "_LMiddle1");
                _cachedMecanimBoneNameMap.Add("Left Middle Intermediate", assetName + "_LMiddle2");
                _cachedMecanimBoneNameMap.Add("Left Middle Distal", assetName + "_LMiddle3");
                _cachedMecanimBoneNameMap.Add("Right Middle Proximal", assetName + "_RMiddle1");
                _cachedMecanimBoneNameMap.Add("Right Middle Intermediate", assetName + "_RMiddle2");
                _cachedMecanimBoneNameMap.Add("Right Middle Distal", assetName + "_RMiddle3");

                _cachedMecanimBoneNameMap.Add("Left Ring Proximal", assetName + "_LRing1");
                _cachedMecanimBoneNameMap.Add("Left Ring Intermediate", assetName + "_LRing2");
                _cachedMecanimBoneNameMap.Add("Left Ring Distal", assetName + "_LRing3");
                _cachedMecanimBoneNameMap.Add("Right Ring Proximal", assetName + "_RRing1");
                _cachedMecanimBoneNameMap.Add("Right Ring Intermediate", assetName + "_RRing2");
                _cachedMecanimBoneNameMap.Add("Right Ring Distal", assetName + "_RRing3");

                _cachedMecanimBoneNameMap.Add("Left Little Proximal", assetName + "_LPinky1");
                _cachedMecanimBoneNameMap.Add("Left Little Intermediate", assetName + "_LPinky2");
                _cachedMecanimBoneNameMap.Add("Left Little Distal", assetName + "_LPinky3");
                _cachedMecanimBoneNameMap.Add("Right Little Proximal", assetName + "_RPinky1");
                _cachedMecanimBoneNameMap.Add("Right Little Intermediate", assetName + "_RPinky2");
                _cachedMecanimBoneNameMap.Add("Right Little Distal", assetName + "_RPinky3");
                break;
            case BoneNameConvention.FBX:
                _cachedMecanimBoneNameMap.Add("Hips", assetName + "_Hips");
                _cachedMecanimBoneNameMap.Add("Spine", assetName + "_Spine");
                _cachedMecanimBoneNameMap.Add("Chest", assetName + "_Spine1");
                _cachedMecanimBoneNameMap.Add("Neck", assetName + "_Neck");
                _cachedMecanimBoneNameMap.Add("Head", assetName + "_Head");
                _cachedMecanimBoneNameMap.Add("LeftShoulder", assetName + "_LeftShoulder");
                _cachedMecanimBoneNameMap.Add("LeftUpperArm", assetName + "_LeftArm");
                _cachedMecanimBoneNameMap.Add("LeftLowerArm", assetName + "_LeftForeArm");
                _cachedMecanimBoneNameMap.Add("LeftHand", assetName + "_LeftHand");
                _cachedMecanimBoneNameMap.Add("RightShoulder", assetName + "_RightShoulder");
                _cachedMecanimBoneNameMap.Add("RightUpperArm", assetName + "_RightArm");
                _cachedMecanimBoneNameMap.Add("RightLowerArm", assetName + "_RightForeArm");
                _cachedMecanimBoneNameMap.Add("RightHand", assetName + "_RightHand");
                _cachedMecanimBoneNameMap.Add("LeftUpperLeg", assetName + "_LeftUpLeg");
                _cachedMecanimBoneNameMap.Add("LeftLowerLeg", assetName + "_LeftLeg");
                _cachedMecanimBoneNameMap.Add("LeftFoot", assetName + "_LeftFoot");
                _cachedMecanimBoneNameMap.Add("RightUpperLeg", assetName + "_RightUpLeg");
                _cachedMecanimBoneNameMap.Add("RightLowerLeg", assetName + "_RightLeg");
                _cachedMecanimBoneNameMap.Add("RightFoot", assetName + "_RightFoot");
                _cachedMecanimBoneNameMap.Add("LeftToes", assetName + "_LeftToeBase");
                _cachedMecanimBoneNameMap.Add("RightToes", assetName + "_RightToeBase");
                break;
            case BoneNameConvention.BVH:
                _cachedMecanimBoneNameMap.Add("Hips", assetName + "_Hips");
                _cachedMecanimBoneNameMap.Add("Spine", assetName + "_Chest");
                _cachedMecanimBoneNameMap.Add("Chest", assetName + "_Chest2");
                _cachedMecanimBoneNameMap.Add("Neck", assetName + "_Neck");
                _cachedMecanimBoneNameMap.Add("Head", assetName + "_Head");
                _cachedMecanimBoneNameMap.Add("LeftShoulder", assetName + "_LeftCollar");
                _cachedMecanimBoneNameMap.Add("LeftUpperArm", assetName + "_LeftShoulder");
                _cachedMecanimBoneNameMap.Add("LeftLowerArm", assetName + "_LeftElbow");
                _cachedMecanimBoneNameMap.Add("LeftHand", assetName + "_LeftWrist");
                _cachedMecanimBoneNameMap.Add("RightShoulder", assetName + "_RightCollar");
                _cachedMecanimBoneNameMap.Add("RightUpperArm", assetName + "_RightShoulder");
                _cachedMecanimBoneNameMap.Add("RightLowerArm", assetName + "_RightElbow");
                _cachedMecanimBoneNameMap.Add("RightHand", assetName + "_RightWrist");
                _cachedMecanimBoneNameMap.Add("LeftUpperLeg", assetName + "_LeftHip");
                _cachedMecanimBoneNameMap.Add("LeftLowerLeg", assetName + "_LeftKnee");
                _cachedMecanimBoneNameMap.Add("LeftFoot", assetName + "_LeftAnkle");
                _cachedMecanimBoneNameMap.Add("RightUpperLeg", assetName + "_RightHip");
                _cachedMecanimBoneNameMap.Add("RightLowerLeg", assetName + "_RightKnee");
                _cachedMecanimBoneNameMap.Add("RightFoot", assetName + "_RightAnkle");
                _cachedMecanimBoneNameMap.Add("LeftToeBase", assetName + "_LeftToe");
                _cachedMecanimBoneNameMap.Add("RightToeBase", assetName + "_RightToe");
                break;
        }
    }
}
