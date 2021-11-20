using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class FreeClimbAnimatorHook : MonoBehaviour
{
    Animator anim;

    IKSnapshot ikBase;
    IKSnapshot current = new IKSnapshot();
    IKSnapshot next = new IKSnapshot();

    public float w_rh;
    public float w_lh;
    public float w_lf;
    public float w_rf;

    Vector3 rh, lh, lf, rf;
    Transform h;
    public void Init(StarterAssets.ThirdPersonController c, Transform helper)
    {
        anim = c._animator;
        ikBase = c.baseIKsnapshot;
        h = helper;

    }

    public void CreatePositions(Vector3 origin)
    {
        IKSnapshot ik = CreateSnapshot(origin);
        CopySnapshot(ref current, ik);

        UpdateIKPosition(AvatarIKGoal.LeftFoot, current.lf);
        UpdateIKPosition(AvatarIKGoal.RightFoot, current.rf);
        UpdateIKPosition(AvatarIKGoal.LeftHand, current.lh);
        UpdateIKPosition(AvatarIKGoal.RightHand, current.rh);

        UpdateIKWeight(AvatarIKGoal.LeftFoot, w_lf);
        UpdateIKWeight(AvatarIKGoal.RightFoot, w_rf);
        UpdateIKWeight(AvatarIKGoal.LeftHand, w_lh);
        UpdateIKWeight(AvatarIKGoal.RightHand, w_rh);

    }
    public IKSnapshot CreateSnapshot(Vector3 o)
    {
        IKSnapshot r = new IKSnapshot();
        r.lh = LocalToWorld(ikBase.lh);
        r.rh = LocalToWorld(ikBase.rh);
        r.lf = LocalToWorld(ikBase.lf);
        r.rf = LocalToWorld(ikBase.rf);

        return r;
    }

    Vector3 LocalToWorld(Vector3 p)
    {
        Vector3 r = h.position;
        r += h.right * p.x;
        r += h.forward * p.z;
        r += h.up * p.y;
        return r;
    }

    public void CopySnapshot(ref IKSnapshot to, IKSnapshot from)
    {
        to.rh = from.rh;
        to.lh = from.lh;
        to.rf = from.rf;
        to.lf = from.lf;
    }

    public void UpdateIKPosition(AvatarIKGoal goal, Vector3 pos)
    {
        switch (goal)
        {
            case AvatarIKGoal.LeftHand:
                lh = pos;
                break;
            case AvatarIKGoal.RightHand:
                rh = pos;
                break;
            case AvatarIKGoal.LeftFoot:
                lf = pos;
                break;
            case AvatarIKGoal.RightFoot:
                rf = pos;
                break;
            default:
                break;
        }

    }

    public void UpdateIKWeight(AvatarIKGoal goal, float w)
    {
        switch (goal)
        {
            case AvatarIKGoal.LeftHand:
                w_lh = w;
                break;
            case AvatarIKGoal.RightHand:
                w_rh = w;
                break;
            case AvatarIKGoal.LeftFoot:
                w_lf = w;
                break;
            case AvatarIKGoal.RightFoot:
                w_rf = w;
                break;
            default:
                break;
        }

    }

    void OnAnimatorIK()
    {
        SetIKPos(AvatarIKGoal.LeftHand, lh, w_lf);
        SetIKPos(AvatarIKGoal.RightHand, rh, w_rf);
        SetIKPos(AvatarIKGoal.LeftFoot, lf, w_lh);
        SetIKPos(AvatarIKGoal.RightFoot, rf, w_rh);
    }

    void SetIKPos(AvatarIKGoal goal, Vector3 targetPosition, float w)
    {
        anim.SetIKPositionWeight(goal, w);
        anim.SetIKPosition(goal, targetPosition);
    }

}
