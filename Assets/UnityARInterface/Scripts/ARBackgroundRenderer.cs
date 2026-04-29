using UnityEngine;
using UnityEngine.Rendering;

public enum ARRenderMode
{
    StandardBackground = 0,
    MaterialAsBackground = 1,
}

/// <summary>
/// Lightweight compatibility shim for legacy Unity AR helper code that used
/// Unity's old ARBackgroundRenderer utility type.
/// </summary>
public sealed class ARBackgroundRenderer
{
    private const CameraEvent BackgroundCameraEvent = CameraEvent.BeforeForwardOpaque;

    private Camera attachedCamera;
    private Material attachedMaterial;
    private ARRenderMode renderMode;
    private CommandBuffer commandBuffer;

    public Camera camera
    {
        get => attachedCamera;
        set
        {
            if (ReferenceEquals(attachedCamera, value))
            {
                return;
            }

            Detach();
            attachedCamera = value;
            Refresh();
        }
    }

    public Material backgroundMaterial
    {
        get => attachedMaterial;
        set
        {
            if (ReferenceEquals(attachedMaterial, value))
            {
                return;
            }

            attachedMaterial = value;
            Refresh();
        }
    }

    public ARRenderMode mode
    {
        get => renderMode;
        set
        {
            if (renderMode == value)
            {
                return;
            }

            renderMode = value;
            Refresh();
        }
    }

    private void Refresh()
    {
        Detach();

        if (attachedCamera == null ||
            attachedMaterial == null ||
            renderMode != ARRenderMode.MaterialAsBackground)
        {
            return;
        }

        commandBuffer = new CommandBuffer
        {
            name = "ARBackgroundRenderer"
        };
        commandBuffer.Blit(null, BuiltinRenderTextureType.CurrentActive, attachedMaterial);
        attachedCamera.AddCommandBuffer(BackgroundCameraEvent, commandBuffer);
    }

    private void Detach()
    {
        if (attachedCamera != null && commandBuffer != null)
        {
            attachedCamera.RemoveCommandBuffer(BackgroundCameraEvent, commandBuffer);
        }

        if (commandBuffer != null)
        {
            commandBuffer.Release();
            commandBuffer = null;
        }
    }
}
