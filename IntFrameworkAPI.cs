using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace IntFramework
{
    public static class IntFrameworkAPI
    {
        private const string name = "intFramework";
        private const string author = "intTeam";
        private const string ugcId = "3207475761";

        public static void Initialize()
            => BackgroundItemLoader.Instance.StartCoroutine(InitializeRoutine());
        private static IEnumerator InitializeRoutine()
        {
            metadata = ModAPI.Metadata;
            var refResponsed = new RefBoolean();
            FindFramework(GetModEntries(), refResponsed);
            yield return new WaitUntil(() => refResponsed.Value);
            if (!frameworkEntry)
                yield break;

            if (!frameworkEntry.ModMeta.Active)
            {
                var responsed = false;
                UISoundBehaviour.Main.Warning();
                DialogBoxManager.Dialog($"[{metadata.Name}]\nintFramework disabled.", new DialogButton[]
                {
                    new DialogButton("Ignore", true, () => responsed = true),
                    new DialogButton("Enable", true, () =>
                    {
                        ModLoader.SetModActive(frameworkEntry.ModMeta);
                        frameworkEntry.UpdateUi();
                        responsed = true;
                    })
                });

                yield return new WaitUntil(() => responsed);

                if (!frameworkEntry.ModMeta.Active)
                    yield break;
            }

            yield return new WaitForEndOfFrame();

            try
            {
                api = ModLoader.ModScripts[frameworkEntry.ModMeta].LoadedAssembly.GetType("IntFramework.IntFrameworkAPI");
            }
            catch (Exception exception)
            {
                UISoundBehaviour.Main.Error();
                DialogBoxManager.Dialog($"[{metadata.Name}]\nFailed initialize intFramework: {exception}");
                yield break;
            }
            initialized = true;

            foreach ((string methodName, object[] invokeArgs) in methodsToInvoke)
                InvokeMethod(methodName, invokeArgs);
        }
        private static void FindFramework(List<ModEntryBehaviour> modEntries, RefBoolean responsed)
        {
            if (!TryFindFramework(modEntries))
            {
                UISoundBehaviour.Main.Error();
                DialogBoxManager.Dialog($"[{metadata.Name}]\nFramework not installed or has error.", new DialogButton[]
                {
                    new DialogButton("Ignore", true, () => responsed.Value = true),
                    new DialogButton("Retry", true, async() =>
                    {
                        await Task.Delay(30000);
                        FindFramework(modEntries, responsed);
                    })
                });
            }
            else
            {
                responsed.Value = true;
            }    
        }
        private static bool TryFindFramework(List<ModEntryBehaviour> modEntries)
        {
            foreach (var modEntry in modEntries)
            {
                if (modEntry.ModMeta == null || modEntry.ModMeta.HasErrors)
                    continue;
                if (modEntry.ModMeta.Name == name && modEntry.ModMeta.Author == author)
                    if (modEntry.ModMeta.CreatorUGCIdentity == ugcId)
                    {
                        frameworkEntry = modEntry;
                        break;
                    }
                    else if (frameworkEntry == null || (modEntry.ModMeta.Active && !frameworkEntry.ModMeta.Active))
                        frameworkEntry = modEntry;
            }
            return frameworkEntry;
        }
        private static List<ModEntryBehaviour> GetModEntries()
        {
            var modList = GameObject.Find("Canvas/Mods").GetComponent<ModListBehaviour>();
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var modEntriesField = typeof(ModListBehaviour).GetField("modEntries", flags);
            return (List<ModEntryBehaviour>)modEntriesField.GetValue(modList);
        }
        public static void RegisterPieceOfClothing(PieceOfClothingProperties properties)
            => TryInvokeMethod("RegisterPieceOfClothing", metadata, properties);
        private static void TryInvokeMethod(string name, params object[] args)
        {
            if (initialized)
                InvokeMethod(name, args);
            else
                methodsToInvoke.Add((name, args));
        }
        private static void InvokeMethod(string name, object[] args)
            => api.InvokeMember(name, System.Reflection.BindingFlags.InvokeMethod, null, null, args);

        private static ModMetaData metadata;
        private static ModEntryBehaviour frameworkEntry;
        private static Type api;
        private static bool initialized;
        private static readonly List<(string, object[])> methodsToInvoke = new List<(string, object[])>();

        private class RefBoolean
        {
            public RefBoolean(bool value = false)
                => Value = value;

            public bool Value;
        }
    }

    public class PieceOfClothingProperties
    {
        protected event Action<SubPieceOfClothingProperties> OnSubPieceAdded;

        public PieceOfClothingProperties()
        {
        }
        public PieceOfClothingProperties(string name)
            => Name = name;
        public PieceOfClothingProperties WithName(string name)
        {
            Name = name;
            return this;
        }
        public PieceOfClothingProperties WithDescription(string description)
        {
            Description = description;
            return this;
        }
        public PieceOfClothingProperties WithNameToOrderBy(string nameToOrderBy)
        {
            NameToOrderBy = nameToOrderBy;
            return this;
        }
        public PieceOfClothingProperties WithThumbnail(Sprite thumbnail)
        {
            Thumbnail = thumbnail;
            return this;
        }
        public PieceOfClothingProperties WithThumbnail(string path)
            => WithThumbnail(ModAPI.LoadSprite(path));
        public PieceOfClothingProperties WithCategory(string categoryName)
        {
            CategoryName = categoryName;
            return this;
        }
        public PieceOfClothingProperties WithType(PersonType type)
        {
            PersonType = type;
            return this;
        }
        public PieceOfClothingProperties AddSubPieceOfClothing(SubPieceOfClothingProperties subPiece)
        {
            OnSubPieceAdded?.Invoke(subPiece);
            SubPieces.Add(subPiece);
            return this;
        }
        public PieceOfClothingProperties AddDefaultColor(Color color)
        {
            DefaultColors.Add(color);
            return this;
        }
        public PieceOfClothingProperties AddDefaultColor(float r, float g, float b, float a = 1f)
            => AddDefaultColor(new Color(r, g, b, a));
        public PieceOfClothingProperties AddDefaultColor(byte r, byte g, byte b, byte a = 255)
            => AddDefaultColor(new Color(r / 255f, g / 255f, b / 255f, a / 255f));
        public PieceOfClothingProperties WithColorable()
        {
            Colorable = true;
            return this;
        }

        public string Name;
        public string Description;
        public string NameToOrderBy;
        public Sprite Thumbnail;
        public string CategoryName;
        public PersonType PersonType;
        public List<SubPieceOfClothingProperties> SubPieces = new List<SubPieceOfClothingProperties>();
        public List<Color> DefaultColors = new List<Color>();
        public bool Colorable;
    }

    [Flags]
    public enum PersonType
    {
        Human,
        Android,
        Gorse
    }

    public class SubPieceOfClothingProperties
    {
        public SubPieceOfClothingProperties WithType(PieceOfClothingType type)
        {
            TypeName = type.ToString();
            this.type = type;
            return this;
        }
        public SubPieceOfClothingProperties AddCloth(ClothProperties cloth)
        {
            Cloths.Add(cloth);
            return this;
        }
        public SubPieceOfClothingProperties WithThickness(float thickness)
        {
            Thickness = thickness;
            return this;
        }
        public SubPieceOfClothingProperties WithCanBeAttachedOnFront()
        {
            CanBeAttachedOnFront = true;
            return this;
        }
        public SubPieceOfClothingProperties WithFrontClone()
        {
            HasFrontClone = true;
            return this;
        }

        public PieceOfClothingType Type
        {
            get => type;
            set
            {
                TypeName = value.ToString();
                type = value;
            }
        }

        public string TypeName;
        private PieceOfClothingType type;
        public List<ClothProperties> Cloths = new List<ClothProperties>();
        public float Thickness;
        public bool CanBeAttachedOnFront;
        public bool HasFrontClone;
    }

    public enum PieceOfClothingType
    {
        Headdress,
        Glasses,
        Body,
        Wrist,
        Leg,
        Foot
    }

    public class ClothProperties
    {
        public ClothProperties WithPiece(Limb piece)
        {
            PieceName = piece.ToString();
            this.piece = piece;
            return this;
        }
        public ClothProperties WithSprite(Sprite sprite)
        {
            Sprite = sprite;
            return this;
        }
        public ClothProperties WithSprite(string path)
            => WithSprite(ModAPI.LoadSprite(path));
        public ClothProperties WithMaskSprite(Sprite sprite)
        {
            MaskSprite = sprite;
            return this;
        }
        public ClothProperties WithMaskSprite(string path)
            => WithMaskSprite(ModAPI.LoadSprite(path));
        public ClothProperties WithMass(float mass)
        {
            Mass = mass;
            return this;
        }
        public ClothProperties WithPhysicalProperties(PhysicalProperties properties)
        {
            PhysicalProperties = properties;
            return this;
        }
        public ClothProperties WithPhysicalProperties(string name)
            => WithPhysicalProperties(ModAPI.FindPhysicalProperties(name));
        public ClothProperties WithPhysicalProperties(float mass, PhysicalProperties properties)
            => WithMass(mass)
                .WithPhysicalProperties(properties);
        public ClothProperties WithPhysicalProperties(float mass, string propertiesName)
            => WithMass(mass)
                .WithPhysicalProperties(propertiesName);
        public ClothProperties WithAttachOffset(Vector3 offset)
        {
            AttachOffset = offset;
            return this;
        }
        public ClothProperties WithAttachOffset(float x, float y, float z = 0f)
            => WithAttachOffset(new Vector3(x, y, z));
        public ClothProperties WithInstantDestruction()
        {
            InstantDestruction = true;
            return this;
        }
        public ClothProperties WithCollisionOnAttach()
        {
            HasCollisionOnAttach = true;
            return this;
        }
        public ClothProperties WithFrontClone()
        {
            HasFrontClone = true;
            return this;
        }

        public Limb Piece
        {
            get => piece;
            set
            {
                PieceName = value.ToString();
                piece = value;
            }
        }

        public string PieceName;
        private Limb piece;
        public Sprite Sprite;
        public Sprite MaskSprite;
        public float Mass = 0.01f;
        public PhysicalProperties PhysicalProperties;
        public Vector3 AttachOffset;
        public bool InstantDestruction;
        public bool HasCollisionOnAttach;
        public bool HasFrontClone;
    }

    public enum Limb
    {
        Head,
        UpperBody,
        MiddleBody,
        LowerBody,
        UpperLegFront,
        LowerLegFront,
        FootFront,
        UpperLeg,
        LowerLeg,
        Foot,
        UpperArmFront,
        LowerArmFront,
        UpperArm,
        LowerArm
    }
}