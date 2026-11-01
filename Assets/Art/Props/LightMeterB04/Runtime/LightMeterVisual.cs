using System;
using UnityEngine;

namespace ThatGamejam.Art
{
    /// <summary>Presentation only. The caller owns metering, timing, ISO state and persistence.</summary>
    [DisallowMultipleComponent]
    public sealed class LightMeterVisual : MonoBehaviour
    {
        [SerializeField] private Transform motionPivot;
        [SerializeField] private Transform isoDial;
        [SerializeField] private Transform readingNeedle;
        [SerializeField] private Renderer readoutRenderer;
        [SerializeField] private int readoutMaterialIndex;
        [SerializeField] private Texture blankReadoutTexture;

        private MaterialPropertyBlock propertyBlock;
        private static readonly int MainTexture = Shader.PropertyToID("_MainTex");

        // Captures actual presentation channels, including poses sampled by AnimationClip.
        // This is a transient object, not a new gameplay/save schema.
        public sealed class Snapshot
        {
            internal Vector3 Position;
            internal Quaternion MotionRotation, IsoRotation, NeedleRotation;
            internal bool NeedleVisible, ContentVisible;
            internal Texture Content;
        }

        public void SetHeldProgress(float progress)
        {
            RequireReferences();
            progress = ClampFinite(progress, nameof(progress));
            motionPivot.localPosition = new Vector3(0f, .14f * progress, -.035f * progress);
            motionPivot.localRotation = Quaternion.Euler(-18f * progress, 0f, 0f);
        }

        public void SetIsoIndex(int index)
        {
            RequireReferences();
            if (index < 0 || index > 3) throw new ArgumentOutOfRangeException(nameof(index));
            isoDial.localRotation = Quaternion.Euler(0f, 0f, index * 45f);
        }

        /// <summary>value is a normalized display position, not EV, lux or exposure. Content is caller-owned.</summary>
        public void SetReadingNormalized(float value, bool available, Texture content = null)
        {
            RequireReferences();
            if (available)
            {
                value = ClampFinite(value, nameof(value));
                readingNeedle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-55f, 55f, value));
            }
            readingNeedle.gameObject.SetActive(available);
            readoutRenderer.enabled = available;
            SetContent(available ? content : null);
        }

        public void ClearReading()
        {
            SetReadingNormalized(0f, false);
        }

        public Snapshot Capture()
        {
            RequireReferences();
            ReadBlock();
            return new Snapshot
            {
                Position = motionPivot.localPosition,
                MotionRotation = motionPivot.localRotation,
                IsoRotation = isoDial.localRotation,
                NeedleRotation = readingNeedle.localRotation,
                NeedleVisible = readingNeedle.gameObject.activeSelf,
                ContentVisible = readoutRenderer.enabled,
                Content = propertyBlock.GetTexture(MainTexture)
            };
        }

        public void Restore(Snapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            RequireReferences();
            motionPivot.localPosition = snapshot.Position;
            motionPivot.localRotation = snapshot.MotionRotation;
            isoDial.localRotation = snapshot.IsoRotation;
            readingNeedle.localRotation = snapshot.NeedleRotation;
            readingNeedle.gameObject.SetActive(snapshot.NeedleVisible);
            readoutRenderer.enabled = snapshot.ContentVisible;
            SetContent(snapshot.Content);
        }

        private void ReadBlock()
        {
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            readoutRenderer.GetPropertyBlock(propertyBlock, readoutMaterialIndex);
        }

        private void SetContent(Texture texture)
        {
            ReadBlock();
            propertyBlock.SetTexture(MainTexture, texture != null ? texture : blankReadoutTexture);
            readoutRenderer.SetPropertyBlock(propertyBlock, readoutMaterialIndex);
        }

        private static float ClampFinite(float value, string argument)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) throw new ArgumentOutOfRangeException(argument);
            return Mathf.Clamp01(value);
        }

        private void RequireReferences()
        {
            if (motionPivot == null || isoDial == null || readingNeedle == null || readoutRenderer == null ||
                blankReadoutTexture == null || readoutMaterialIndex < 0 || readoutMaterialIndex >= readoutRenderer.sharedMaterials.Length)
                throw new InvalidOperationException("LightMeterVisual requires the references supplied by PF_LightMeter_B04.");
        }
    }
}
