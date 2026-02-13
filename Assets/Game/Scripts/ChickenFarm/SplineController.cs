using UnityEngine;
using System;
using DG.Tweening;
using Dreamteck.Splines;

public class SplineController : MonoBehaviour
{
    private SplineFollower follower;

    public void Init(SplineComputer path, Vector3 targetPos, Action callback)
    {
        follower = GetComponent<SplineFollower>();

        if (follower != null)
        {
            follower.spline = path; // Hayvanýn özel yolu atandý
            follower.RebuildImmediate(); // Yolu hemen güncelle
            follower.SetPercent(0); // Baþlangýca sar
            follower.follow = true;

            follower.onEndReached += (double val) => {
                OnSplineEnd(targetPos, callback);
            };
        }
        else
        {
            callback?.Invoke();
        }
    }

    private void OnSplineEnd(Vector3 targetPos, Action callback)
    {
        follower.follow = false;

        // Spline bitiþi -> Slota atlayýþ
        transform.DOJump(targetPos, 1.2f, 1, 0.5f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => callback?.Invoke());

        transform.DORotate(new Vector3(0, 360, 0), 0.5f, RotateMode.FastBeyond360);
    }
}