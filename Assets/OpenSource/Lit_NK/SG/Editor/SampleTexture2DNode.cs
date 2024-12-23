using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.NKStudio.ShaderGraph
{
    enum TextureType
    {
        Default,
        Normal
    };

    [FormerName("UnityEditor.ShaderGraph.Texture2DNodeAdvanced")]
    [Title("Input", "Texture", "Sample Texture 2D Advanced")]
    class SampleTexture2DNode : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV
    {
        public const int OutputSlotRGBAId = 0;
        public const int OutputSlotRGBId = 5;
        public const int OutputSlotRId = 6;
        public const int OutputSlotGId = 7;
        public const int OutputSlotBId = 8;
        public const int OutputSlotAId = 9;
        
        public const int TextureInputId = 1;
        public const int TextureSTInputId = 2;
        public const int UVInput = 3;
        public const int SamplerInput = 4;

        const string kOutputSlotRGBAName = "RGBA";
        const string kOutputSlotRGBName = "RGB";
        const string kOutputSlotRName = "R";
        const string kOutputSlotGName = "G";
        const string kOutputSlotBName = "B";
        const string kOutputSlotAName = "A";
        const string kTextureInputName = "Texture";
        const string kTextureSTInputName = "Texture_ST";
        const string kUVInputName = "UV";
        const string kSamplerInputName = "Sampler";
        const string kDefaultSampleMacro = "SAMPLE_TEXTURE2D";
        const string kSampleMacroNoBias = "PLATFORM_SAMPLE_TEXTURE2D";


        public override bool hasPreview { get { return true; } }

        public SampleTexture2DNode()
        {
            name = "Sample Texture 2D Advanced";
            UpdateNodeAfterDeserialization();
        }

        [SerializeField]
        private TextureType m_TextureType = TextureType.Default;

        [EnumControl("Type")]
        public TextureType textureType
        {
            get { return m_TextureType; }
            set
            {
                if (m_TextureType == value)
                    return;

                m_TextureType = value;
                Dirty(ModificationScope.Graph);

                ValidateNode();
            }
        }

        [SerializeField]
        private NormalMapSpace m_NormalMapSpace = NormalMapSpace.Tangent;

        [EnumControl("Space")]
        public NormalMapSpace normalMapSpace
        {
            get { return m_NormalMapSpace; }
            set
            {
                if (m_NormalMapSpace == value)
                    return;

                m_NormalMapSpace = value;
                Dirty(ModificationScope.Graph);
            }
        }

        [SerializeField]
        private bool m_EnableGlobalMipBias = true;
        internal bool enableGlobalMipBias
        {
            set { m_EnableGlobalMipBias = value; }
            get { return m_EnableGlobalMipBias; }
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector4MaterialSlot(OutputSlotRGBAId, kOutputSlotRGBAName, kOutputSlotRGBAName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector3MaterialSlot(OutputSlotRGBId, kOutputSlotRGBName, kOutputSlotRGBName, SlotType.Output, Vector3.zero, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OutputSlotRId, kOutputSlotRName, kOutputSlotRName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OutputSlotGId, kOutputSlotGName, kOutputSlotGName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OutputSlotBId, kOutputSlotBName, kOutputSlotBName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Vector1MaterialSlot(OutputSlotAId, kOutputSlotAName, kOutputSlotAName, SlotType.Output, 0, ShaderStageCapability.Fragment));
            AddSlot(new Texture2DInputMaterialSlot(TextureInputId, kTextureInputName, kTextureInputName));
            AddSlot(new Texture2DInputMaterialSlot(TextureSTInputId, kTextureSTInputName, kTextureSTInputName));
            AddSlot(new UVMaterialSlot(UVInput, kUVInputName, kUVInputName, UVChannel.UV0));
            AddSlot(new SamplerStateMaterialSlot(SamplerInput, kSamplerInputName, kSamplerInputName, SlotType.Input));
            RemoveSlotsNameNotMatching(new[] { OutputSlotRGBAId, OutputSlotRGBId, OutputSlotRId, OutputSlotGId, OutputSlotBId, OutputSlotAId, TextureInputId, TextureSTInputId, UVInput, SamplerInput });
        }

        public override void Setup()
        {
            base.Setup();
            var textureSlot = FindInputSlot<Texture2DInputMaterialSlot>(TextureInputId);
            textureSlot.defaultType = (textureType == TextureType.Normal ? Texture2DShaderProperty.DefaultType.NormalMap : Texture2DShaderProperty.DefaultType.White);
        }

        // Node generations
        public virtual void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var uvName = GetSlotValue(UVInput, generationMode);

            //Sampler input slot
            var samplerSlot = FindInputSlot<MaterialSlot>(SamplerInput);
            var edgesSampler = owner.GetEdges(samplerSlot.slotReference);

            //Sampler input slot
            var textureSTSlot = FindInputSlot<MaterialSlot>(TextureSTInputId);
            var edgesST = owner.GetEdges(textureSTSlot.slotReference);
            
            var id = GetSlotValue(TextureInputId, generationMode);
            var result = string.Format("$precision4 {0} = {1}({2}.tex, {3}.samplerstate, {5}.GetTransformedUV({4}));"
                , GetVariableNameForSlot(OutputSlotRGBAId)
                , m_EnableGlobalMipBias ? kDefaultSampleMacro : kSampleMacroNoBias
                , id
                , edgesSampler.Any() ? GetSlotValue(SamplerInput, generationMode) : id
                , uvName
                , edgesST.Any() ? GetSlotValue(TextureSTInputId, generationMode) : id);

            sb.AppendLine(result);

            if (textureType == TextureType.Normal)
            {
                if (normalMapSpace == NormalMapSpace.Tangent)
                {
                    sb.AppendLine(string.Format("{0}.rgb = UnpackNormal({0});", GetVariableNameForSlot(OutputSlotRGBAId)));
                }
                else
                {
                    sb.AppendLine(string.Format("{0}.rgb = UnpackNormalRGB({0});", GetVariableNameForSlot(OutputSlotRGBAId)));
                }
            }

            sb.AppendLine(string.Format("$precision3 {0} = {1}.rgb;", GetVariableNameForSlot(OutputSlotRGBId), GetVariableNameForSlot(OutputSlotRGBAId)));
            sb.AppendLine(string.Format("$precision {0} = {1}.r;", GetVariableNameForSlot(OutputSlotRId), GetVariableNameForSlot(OutputSlotRGBAId)));


            sb.AppendLine(string.Format("$precision {0} = {1}.g;", GetVariableNameForSlot(OutputSlotGId), GetVariableNameForSlot(OutputSlotRGBAId)));
            sb.AppendLine(string.Format("$precision {0} = {1}.b;", GetVariableNameForSlot(OutputSlotBId), GetVariableNameForSlot(OutputSlotRGBAId)));
            sb.AppendLine(string.Format("$precision {0} = {1}.a;", GetVariableNameForSlot(OutputSlotAId), GetVariableNameForSlot(OutputSlotRGBAId)));
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                var result = false;
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresMeshUV(channel))
                    {
                        result = true;
                        break;
                    }
                }

                tempSlots.Clear();
                return result;
            }
        }
    }
}
