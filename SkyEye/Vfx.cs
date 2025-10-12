using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Types;

namespace SkyEye.SkyEye;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct VfxStruct {
    [FieldOffset(0x38)] public byte Flags;
    [FieldOffset(0x50)] public Vector3 Position;
    [FieldOffset(0x60)] public Quat Rotation;
    [FieldOffset(0x70)] public Vector3 Scale;

    [FieldOffset(0x128)] public int ActorCaster;
    [FieldOffset(0x130)] public int ActorTarget;

    [FieldOffset(0x1B8)] public int StaticCaster;
    [FieldOffset(0x1C0)] public int StaticTarget;
}

public abstract unsafe class BaseVfx {
    public VfxStruct* Vfx;
    public string Path;
    public static readonly Dictionary<BaseVfx, VfxSpawnItem> Vfxs = [];

    public delegate IntPtr StaticVfxCreateDelegate(string path, string pool);

    public static StaticVfxCreateDelegate? StaticVfxCreate;

    public delegate IntPtr StaticVfxRunDelegate(IntPtr vfx, float a1, uint a2);

    public static StaticVfxRunDelegate? StaticVfxRun;

    public delegate IntPtr ActorVfxRemoveDelegate(IntPtr vfx, char a2);

    public ActorVfxRemoveDelegate ActorVfxRemove;
    public delegate IntPtr ActorVfxCreateDelegate( string path, IntPtr a2, IntPtr a3, float a4, char a5, ushort a6, char a7 );

    public ActorVfxCreateDelegate ActorVfxCreate;
    public delegate IntPtr StaticVfxRemoveDelegate( IntPtr vfx );

    public StaticVfxRemoveDelegate StaticVfxRemove;

    public BaseVfx(string path) {
        Path = path;
        ActorVfxCreate = Marshal.GetDelegateForFunctionPointer<ActorVfxCreateDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? B0 02 EB 02"));
        ActorVfxRemove = Marshal.GetDelegateForFunctionPointer<ActorVfxRemoveDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? B0 02 EB 02"));
        StaticVfxRemove = Marshal.GetDelegateForFunctionPointer<StaticVfxRemoveDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? B0 02 EB 02"));
        StaticVfxRun ??= Marshal.GetDelegateForFunctionPointer<StaticVfxRunDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? B0 02 EB 02"));
        StaticVfxCreate ??= Marshal.GetDelegateForFunctionPointer<StaticVfxCreateDelegate>(Plugin.SigScanner.ScanText("E8 ?? ?? ?? ?? F3 0F 10 35 ?? ?? ?? ?? 48 89 43 08"));
    }

    public abstract void Remove();

    public void Update() {
        if (Vfx == null) return;
        Vfx->Flags |= 0x2;
    }

    public void UpdatePosition(Vector3 position) {
        if (Vfx == null) return;
        Vfx->Position = new Vector3 {
            X = position.X,
            Y = position.Y,
            Z = position.Z
        };
    }

    public void UpdatePosition(IGameObject actor) {
        if (Vfx == null) return;
        Vfx->Position = actor.Position;
    }

    public void UpdateScale(Vector3 scale) {
        if (Vfx == null) return;
        Vfx->Scale = new Vector3 {
            X = scale.X,
            Y = scale.Y,
            Z = scale.Z
        };
    }

    public void UpdateRotation(Vector3 rotation) {
        if (Vfx == null) return;

        var q = Quaternion.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z);
        Vfx->Rotation = new Quat {
            X = q.X,
            Y = q.Y,
            Z = q.Z,
            W = q.W
        };
    }
}

[StructLayout(LayoutKind.Sequential)]
public struct Quat {
    public float X;
    public float Z;
    public float Y;
    public float W;

    public static implicit operator System.Numerics.Vector4(Quat pos) => new(pos.X, pos.Y, pos.Z, pos.W);

    // public static implicit operator SharpDX.Vector4( Quat pos ) => new( pos.X, pos.Z, pos.Y, pos.W );
}

public unsafe class StaticVfx : BaseVfx {
  

    public StaticVfx(string path, Vector3 position, float rotation) : base(path) {
        Vfx = (VfxStruct*)StaticVfxCreate(path, "Client.System.Scheduler.Instance.VfxObject");
        StaticVfxRun((IntPtr)Vfx, 0.0f, 0xFFFFFFFF);
        UpdatePosition(position);
        UpdateRotation(new Vector3(0, 0, rotation));
        Update();
    }

    public override void Remove() {
        StaticVfxRemove((IntPtr)Vfx);
    }
}

public unsafe class ActorVfx : BaseVfx {
    public ActorVfx(IGameObject caster, IGameObject target, string path) : this(caster.Address, target.Address, path) {
    }

    public ActorVfx(IntPtr caster, IntPtr target, string path) : base(path) {
        Vfx = (VfxStruct*)ActorVfxCreate(path, caster, target, -1, (char)0, 0, (char)0);
    }

    public override void Remove() {
        ActorVfxRemove((IntPtr)Vfx, (char)1);
    }
}

public class VfxSpawnItem {
    public readonly string Path;
    public readonly SpawnType Type;
    public readonly bool CanLoop;

    public VfxSpawnItem(string path, SpawnType type, bool canLoop) {
        Path = path;
        Type = type;
        CanLoop = canLoop;
    }
}

public enum SpawnType {
    None,
    Ground,
    Self,
    Target
}