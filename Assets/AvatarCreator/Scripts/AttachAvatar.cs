using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// using RootMotion.FinalIK;

public class AttachAvatar : MonoBehaviour
{
    [SerializeField]
    private SteamVR_TrackedController _controller;

    [SerializeField]
    private Transform _headTrans;
    
    [SerializeField]
    private Transform _leftHandTrans;

    [SerializeField]
    private Transform _rightHandTrans;

    [SerializeField]
    private Transform _leftFootTrans;

    [SerializeField]
    private Transform _rightFootTrasn;

    [SerializeField]
    private Animator _target;

    [SerializeField]
    private bool _useFootIK = false;

    // private VRIK _vrik;
    private AvatarSkeleton _avatarSkeleton;

    private Quaternion _initHeadRot;
    private Quaternion _initLeftHandRot;
    private Quaternion _initRightHandRot;
    private Quaternion _initLeftFootRot;
    private Quaternion _initRightFootRot;

    private void Awake()
    {
        // _vrik = GetComponentInChildren<VRIK>(true);
        _avatarSkeleton = GetComponent<AvatarSkeleton>();
        _controller.TriggerClicked += OnTriggetClickedHandler;

        _initHeadRot = _headTrans.rotation;
        _initLeftHandRot = _leftHandTrans.rotation;
        _initRightHandRot = _rightHandTrans.rotation;

        if (_useFootIK)
        {
            _initLeftFootRot = _leftFootTrans.rotation;
            _initRightFootRot = _rightFootTrasn.rotation;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Attach();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            _avatarSkeleton.RemoveTarget(_target);
        }
    }

    /// <summary>
    /// VRコントローラのトリガークリックハンドラ
    /// </summary>
    private void OnTriggetClickedHandler(object sender, ClickedEventArgs e)
    {
        Attach();
    }

    /// <summary>
    /// アバターをアタッチする
    /// </summary>
    private void Attach()
    {
        // 手首の回転など、初期のターゲット回転に戻してからアタッチ、キャリブレーションする
        _headTrans.rotation = _initHeadRot;
        _leftHandTrans.rotation = _initLeftHandRot;
        _rightHandTrans.rotation = _initRightHandRot;

        if (_useFootIK)
        {
            _leftFootTrans.rotation = _initLeftFootRot;
            _rightFootTrasn.rotation = _initRightFootRot;
        }

        // キャリブレーションとアバターの生成
        if (_useFootIK)
        {
            _avatarSkeleton.Calibration(_headTrans, _leftHandTrans, _rightHandTrans, _leftFootTrans, _rightFootTrasn);
        }
        else
        {
            _avatarSkeleton.Calibration(_headTrans, _leftHandTrans, _rightHandTrans);
        }

        _avatarSkeleton.Create();

        // VRIKにターゲット登録、および有効化
        // _vrik.solver.spine.headTarget = _headTrans;
        // _vrik.solver.leftArm.target = _leftHandTrans;
        // _vrik.solver.rightArm.target = _rightHandTrans;
        //
        // if (_useFootIK)
        // {
        //     _vrik.solver.leftLeg.target = _leftFootTrans;
        //     _vrik.solver.leftLeg.positionWeight = 1f;
        //     _vrik.solver.leftLeg.rotationWeight = 1f;
        //
        //     _vrik.solver.rightLeg.target = _rightFootTrasn;
        //     _vrik.solver.rightLeg.positionWeight = 1f;
        //     _vrik.solver.rightLeg.rotationWeight = 1f;
        // }
        //
        // _vrik.enabled = true;

        _avatarSkeleton.AddTarget(_target);
    }
}
