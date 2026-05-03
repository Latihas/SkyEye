using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using static SkyEye.Data.PData.EurekaWeather;

namespace SkyEye.Data;

public enum Territory : uint {
	Anemos = 732,
	Pagos = 763,
	Pyros = 795,
	Hydatos = 827
}

[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal static class PData {
	internal static readonly Dictionary<Territory, Vector3[]> RabbitTreasurePositions = new() {
		{
			Territory.Pagos, [
				new Vector3(-737.8369f, -677.9246f, 143.3176f),
				new Vector3(-651.9872f, -688.063f, 85.1839f),
				new Vector3(-606.5342f, -700.6216f, 143.02383f),
				new Vector3(-589.4037f, -698.3553f, 15.37505f),
				new Vector3(-430.1872f, -680.5923f, 60.50428f),
				new Vector3(-386.33823f, -677.493f, 468.21628f),
				new Vector3(-340.4522f, -671.272f, 425.2418f),
				new Vector3(-298.347f, -664.6113f, 450.374f),
				new Vector3(-289.462f, -585.3194f, -178.6195f),
				new Vector3(-285.1725f, -586.4724f, -245.163f),
				new Vector3(-283.3895f, -573.506f, -337.2024f),
				new Vector3(-259.90164f, -602.8868f, -139.11661f),
				new Vector3(-257.5845f, -1707f / (float)Math.E, -67.44543f),
				new Vector3(-222.8215f, -557.8751f, -370.6961f),
				new Vector3(-222.4405f, -628.8225f, -69.52717f),
				new Vector3(-199.5562f, -594.9023f, -154.88f),
				new Vector3(-175.5653f, -568.1696f, -237.659f),
				new Vector3(-158.7989f, -576.3018f, -200.3996f),
				new Vector3(-147.99794f, -563.5579f, -282.92133f),
				new Vector3(292.9121f, -640.7648f, 15.70983f),
				new Vector3(351.9018f, -690.0383f, 115.0498f),
				new Vector3(398.6197f, -678.8519f, -15.59305f),
				new Vector3(445.4254f, -741.8608f, 188.4324f),
				new Vector3(476.741f, -739.2648f, 421.0621f),
				new Vector3(487.0211f, -756.6284f, 291.6779f),
				new Vector3(514.9791f, -702.1201f, 139.7072f),
				new Vector3(516.052f, -679.7125f, -37.79275f),
				new Vector3(530.7849f, -720.47f, 230.5152f),
				new Vector3(543.909f, -730.7676f, 355.7726f),
				new Vector3(631.0588f, -730.9005f, 317.3849f),
				new Vector3(643.9543f, -702.8303f, 117.7415f),
				//
				new Vector3(-834.3569f, -612.2924f, -289.3059f),
				new Vector3(-765.2896f, -611.9983f, -159.5746f),
				new Vector3(-693.0172f, -647.2126f, 35.83006f),
				new Vector3(-665.1354f, -627.3637f, -355.8533f),
				new Vector3(-660.2762f, -624.1875f, -221.7502f),
				new Vector3(-631.2686f, -620.137f, -450.9197f),
				new Vector3(-567.9847f, -599.2828f, -580.3564f),
				new Vector3(-531.0604f, -616.7406f, -75.66094f),
				new Vector3(-358.4612f, -648.7422f, -195.9099f),
				new Vector3(-341.6468f, -656.3936f, -87.59322f),
				new Vector3(-60.76746f, -477.3432f, -346.5658f),
				new Vector3(-50.49114f, -462.5427f, -398.8157f),
				new Vector3(51.2085f, -635.3545f, 140.7099f),
				new Vector3(104.5841f, -475.1954f, -341.0629f),
				new Vector3(121.1856f, -460.1292f, -416.6242f),
				new Vector3(147.8505f, -635.129f, 196.705f),
				new Vector3(258.8517f, -641.5285f, 171.4124f),
				new Vector3(300.4921f, -643.3304f, 89.64095f),
				new Vector3(383.1363f, -549.2653f, -180.7012f),
				new Vector3(422.8629f, -578.9005f, -68.52699f),
				new Vector3(434.687f, -531.4767f, -289.311f),
				new Vector3(462.8273f, -552.3704f, -230.8436f),
				new Vector3(536.8254f, -553.566f, -243.9232f),
				new Vector3(562.9125f, -569.653f, -148.1824f),
				new Vector3(583.9424f, -579.3117f, -87.14241f),
				new Vector3(671.0728f, -629.6361f, -242.9718f),
				new Vector3(713.2711f, -630.6937f, -326.9075f),
				new Vector3(734.861f, -630.5897f, -284.3216f),
				new Vector3(817.1915f, -629.0625f, -276.2072f)
			]
		}, {
			Territory.Pyros, [
				new Vector3(-469.8102f, 659.1795f, 441.7094f),
				new Vector3(-464.4484f, 660.6446f, 419.2033f),
				new Vector3(-438.3944f, 660.7888f, 400.7463f),
				new Vector3(-340.1021f, 660.3159f, 384.9267f),
				new Vector3(-198.2953f, 757.9987f, 477.8044f),
				new Vector3(-197.1622f, 759.5239f, 599.0419f),
				new Vector3(-189.47908f, 671.6885f, 323.32883f),
				new Vector3(-150.4335f, 762.666f, 451.7729f),
				new Vector3(-105.5926f, 762.684f, 686.5082f),
				new Vector3(-38.89164f, 769.8203f, 504.8099f),
				new Vector3(-38.375523f, 675.38214f, 354.2082f),
				new Vector3(-11.6334f, 773.301f, 601.2862f),
				new Vector3(2.4903228f, 764.1788f, 411.15732f),
				new Vector3(32.1309f, 754.25977f, 689.94055f),
				new Vector3(92.81797f, 754.2609f, 825.0632f),
				new Vector3(146.3309f, 752.4515f, 756.0107f),
				new Vector3(156.9551f, 751.1077f, 704.1921f),
				new Vector3(157.0789f, 754.6462f, 841.5557f),
				new Vector3(184.4552f, 747.6003f, 617.2846f),
				new Vector3(248.7603f, 723.1207f, 118.5381f),
				new Vector3(280.35367f, 746.5175f, 754.38336f),
				new Vector3(293.8393f, 739.3859f, 531.169f),
				new Vector3(310.4404f, 742.014f, 567.1605f),
				new Vector3(367.9469f, 744.048f, 639.2587f),
				new Vector3(371.0945f, 737.926f, 491.7239f),
				new Vector3(378.1241f, 724.9152f, 287.1851f),
				new Vector3(432.9954f, 731.984f, 568.7686f),
				new Vector3(448.9699f, 725.0576f, 457.0699f),
				new Vector3(460.4148f, 723.1206f, 311.0332f),
				new Vector3(469.0294f, 726.3409f, 535.0562f),
				//
				new Vector3(-628.2433f, 669.827f, -319.8876f),
				new Vector3(-611.952f, 683.3906f, -232.7007f),
				new Vector3(-518.2347f, 680.2383f, -247.2683f),
				new Vector3(-457.8732f, 669.7634f, -367.8863f),
				new Vector3(-412.5399f, 671.2578f, -304.7147f),
				new Vector3(-405.1931f, 666.2214f, -700.382f),
				new Vector3(-404.9317f, 674.0713f, -574.5485f),
				new Vector3(-339.9076f, 670.2526f, -476.6447f),
				new Vector3(-244.1402f, 658.0245f, -691.2558f),
				new Vector3(-233.8513f, 668.9334f, -374.3894f),
				new Vector3(-222.2047f, 658.1601f, -553.8273f),
				new Vector3(-125.8165f, 665.9815f, -333.8782f),
				new Vector3(-70.91751f, 672.8761f, -193.4458f),
				new Vector3(-52.02663f, 660.103f, -299.5649f),
				new Vector3(-43.11547f, 671.0736f, -399.562f),
				new Vector3(-13.58441f, 676.6263f, -706.9113f),
				new Vector3(121.6145f, 685.3226f, -483.2627f),
				new Vector3(148.159f, 687.7542f, -612.4835f),
				new Vector3(206.4645f, 659.4509f, -280.9397f),
				new Vector3(352.7195f, 662.5346f, -249.7375f),
				new Vector3(372.5202f, 659.9396f, -463.6187f),
				new Vector3(421.3589f, 663.7094f, -683.5093f),
				new Vector3(432.8823f, 660.4274f, -228.2652f),
				new Vector3(430.1118f, 671.8463f, -413.4186f),
				new Vector3(447.6103f, 666.0066f, -341.5857f),
				new Vector3(536.2314f, 668.1235f, -473.6654f),
				new Vector3(543.4789f, 668.9618f, -559.8126f),
				new Vector3(549.6398f, 675.0362f, -671.2271f),
				new Vector3(664.5829f, 676.4869f, -104.6262f),
				new Vector3(836.0986f, 656.5436f, -396.1939f)
			]
		}, {
			Territory.Hydatos, [
				new Vector3(-933.6184f, 523.23627f, -736.6334f),
				new Vector3(-920.3497f, 505.9977f, -172.3633f),
				new Vector3(-863.5947f, 513.31824f, -440.51706f),
				new Vector3(-812.4462f, 515.9799f, -936.0333f),
				new Vector3(-799.7302f, 505.52838f, -109.44379f),
				new Vector3(-678.84076f, 504.2658f, -366.4915f),
				new Vector3(-665.98303f, 509.72754f, -589.6073f),
				new Vector3(-614.0915f, 511.94556f, -872.6113f),
				new Vector3(-562.175f, 496.6946f, -13.687779f),
				new Vector3(-438.47156f, 505.1122f, -682.6366f),
				new Vector3(-350.35544f, 500.44604f, -108.943886f),
				new Vector3(-322.95847f, 500.96313f, -730.7684f),
				new Vector3(-182.39871f, 501.35736f, -591.2992f),
				new Vector3(-159.06465f, 495.79843f, -44.438065f),
				new Vector3(8.371242f, 504.53586f, -368.8599f),
				new Vector3(122.10202f, 499.99933f, -616.178f),
				new Vector3(140.43747f, 496.05386f, 12.449963f),
				new Vector3(172.5284f, 503.0239f, -349.57162f),
				new Vector3(226.80516f, 505.52136f, -886.2001f),
				new Vector3(287.7519f, 505.17682f, -519.0092f),
				new Vector3(344.52988f, 495.0796f, -212.5871f),
				new Vector3(368.69495f, 505.93457f, -400.701f),
				new Vector3(467.72028f, 505.08575f, -710.6624f),
				new Vector3(580.4015f, 497.0735f, -80.38016f),
				new Vector3(659.8676f, 502.9848f, -732.67096f),
				new Vector3(671.42645f, 508.26538f, -341.5508f),
				new Vector3(832.6055f, 513.5617f, -520.9632f),
				new Vector3(843.4305f, 494.44394f, -7.3525977f),
				new Vector3(851.3032f, 517.4547f, -883.74927f),
				new Vector3(872.1447f, 518.493f, -237.8645f)
			]
		}
	};
	internal static readonly Dictionary<Territory, Vector3[]> ElementalPositions = new() {
		{
			Territory.Anemos, [
				new Vector3(-47.69984f, 35.005047f, -132.78749f),
				new Vector3(-71.0314f, 40.72186f, -412.3625f),
				new Vector3(-85.73978f, 26.00495f, 191.9998f),
				new Vector3(-121.645805f, 37.443096f, 6.51933f),
				new Vector3(-220.0411f, 43.82002f, -111.9934f),
				new Vector3(-227.09471f, 35.390423f, 121.25729f),
				new Vector3(-241.1971f, 43.741238f, -113.4114f),
				new Vector3(-336.4917f, 63.099617f, -418.0745f),
				new Vector3(-364.4897f, 69.47017f, -276.5466f),
				new Vector3(-373.7721f, 82.72685f, 18.47673f),
				new Vector3(-399.8804f, 17.590218f, 291.9889f),
				new Vector3(-408.8093f, 27.3615f, 425.7613f),
				new Vector3(-415.0757f, 45.343655f, 138.525f),
				new Vector3(-464.80188f, 63.225822f, -435.7054f),
				new Vector3(-580.6493f, 41.84066f, -5.5684004f),
				new Vector3(-588.3321f, 22.778988f, 238.9747f),
				new Vector3(-623.18243f, 45.793133f, -114.6636f),
				new Vector3(-642.79144f, 41.421658f, -119.1096f),
				new Vector3(-732.5733f, 25.723625f, 180.26799f),
				new Vector3(7.8139753f, -23.324959f, 527.6335f),
				new Vector3(8.59974f, 30.267544f, -263.5725f),
				new Vector3(13.26962f, 14.806233f, 300.034f),
				new Vector3(31.99984f, 38.65408f, -19.6001f),
				new Vector3(110.2639f, 27.28825f, 138.9691f),
				new Vector3(132.6454f, -6.9840813f, 465.3566f),
				new Vector3(137.8132f, 37.18542f, -168.3959f),
				new Vector3(225.43289f, 61.958f, 282.7937f),
				new Vector3(239.4716f, 40.204037f, 49.06235f),
				new Vector3(256f, 32.288704f, -330.4921f),
				new Vector3(262.5656f, 30.271093f, -113.7084f),
				new Vector3(339.0738f, 42.00313f, 181.90419f),
				new Vector3(364.5631f, 37.654808f, -410.9562f),
				new Vector3(396.8458f, 33.99593f, -62.532932f),
				new Vector3(405.62952f, 41.094185f, -258.1847f),
				new Vector3(417.74222f, 30.273237f, 306.4423f),
				new Vector3(567.33813f, 35.447132f, -79.25026f),
				new Vector3(643.54285f, 35.356274f, -306.5175f),
				new Vector3(680.3775f, 40.020523f, -1.3000613f),
				new Vector3(686.88556f, 38.07274f, -154.0496f)
			]
		}, {
			Territory.Pagos, [
				new Vector3(-41.5411f, -471.7219f, -365.66238f),
				new Vector3(-93.89765f, -740.93286f, 390.37912f),
				new Vector3(-141.2347f, -562.60614f, -282.6532f),
				new Vector3(-127.999695f, -546.0727f, -101.5866f),
				new Vector3(-181.1255f, -733.01514f, 431.18948f),
				new Vector3(-218.84619f, -720.4173f, 187.913f),
				new Vector3(-224.1831f, -606.68243f, -133.2441f),
				new Vector3(-263.0846f, -569.75006f, -337.29272f),
				new Vector3(-270.0375f, -644.00146f, 14.772899f),
				new Vector3(-286.91522f, -587.29865f, -223.6939f),
				new Vector3(-286.1163f, -677.2271f, 385.7882f),
				new Vector3(-339.81677f, -671.70374f, 160.0002f),
				new Vector3(-432.67923f, -588.9239f, -539.0688f),
				new Vector3(-460.07782f, -703.45056f, 260.35468f),
				new Vector3(-467.4751f, -656.8985f, -26.89501f),
				new Vector3(-479.99982f, -700.4795f, 387.8571f),
				new Vector3(-493.17822f, -611.9679f, -50.352207f),
				new Vector3(-572.7231f, -618.6403f, -167.888f),
				new Vector3(-596.4171f, -620.23083f, -455.2062f),
				new Vector3(-627.7185f, -697.3568f, 147.9566f),
				new Vector3(-677.4056f, -649.40497f, 42.32301f),
				new Vector3(-713.46967f, -624.1894f, -270.3222f),
				new Vector3(0.9566075f, -721.777f, 210.29039f),
				new Vector3(8.574106f, -738.0508f, 323.6507f),
				new Vector3(20.43179f, -546.3381f, 6.304727f),
				new Vector3(87.11651f, -510.49722f, -159.9996f),
				new Vector3(114.5379f, -632.4933f, 119.393f),
				new Vector3(152.0979f, -495.13928f, -282.6871f),
				new Vector3(192.1393f, -647.50696f, 197.5168f),
				new Vector3(273.51492f, -532.7138f, -128.0004f),
				new Vector3(286.3525f, -641.0692f, 25.18766f),
				new Vector3(337.38052f, -721.52655f, 291.58188f),
				new Vector3(354.9172f, -688.889f, 105.6524f),
				new Vector3(426.56082f, -578.40295f, -73.76903f),
				new Vector3(450.2676f, -747.31274f, 217.9324f),
				new Vector3(462.04968f, -553.8097f, -225.24661f),
				new Vector3(489.01028f, -740.3993f, 424.49872f),
				new Vector3(528.40967f, -688.61096f, 50.57122f),
				new Vector3(714.3189f, -630.0619f, -321.2806f),
				new Vector3(735.9999f, -629.8334f, -274.5996f)
			]
		}, {
			Territory.Pyros, [
				new Vector3(-22.74618f, 768.31287f, 510.34402f),
				new Vector3(-33.69695f, 683.36426f, 243.2133f),
				new Vector3(-110.5325f, 764.73047f, 617.9566f),
				new Vector3(-124.4339f, 674.73566f, 305.94412f),
				new Vector3(-153.78471f, 666.87354f, -199.7227f),
				new Vector3(-154.0888f, 764.3937f, 520.66394f),
				new Vector3(-231.1558f, 658.3021f, -558.73425f),
				new Vector3(-248.9779f, 658.02454f, -678.9961f),
				new Vector3(-332.1558f, 641.8102f, 622.6001f),
				new Vector3(-358.3534f, 661.66284f, 343.216f),
				new Vector3(-398.83002f, 666.6412f, -672.0005f),
				new Vector3(-425.1501f, 659.0271f, 447.99982f),
				new Vector3(-439.0166f, 675.91943f, -206.9271f),
				new Vector3(-480.7868f, 673.946f, -345.3124f),
				new Vector3(-596.1195f, 674.65735f, -288.56842f),
				new Vector3(36.22116f, 676.982f, -201.40761f),
				new Vector3(59.199173f, 754.18854f, 702.04144f),
				new Vector3(134.1277f, 675.81714f, -721.8558f),
				new Vector3(145.9791f, 679.15015f, -401.46198f),
				new Vector3(181.1693f, 659.8738f, -278.7961f),
				new Vector3(183.26439f, 751.2846f, 837.4545f),
				new Vector3(197.4814f, 717.4583f, 299.7896f),
				new Vector3(233.8187f, 681.5016f, -584.3393f),
				new Vector3(265.5905f, 745.7182f, 758.2044f),
				new Vector3(272.4338f, 723.1208f, 163.47511f),
				new Vector3(311.00122f, 738.55225f, 510.64432f),
				new Vector3(351.5322f, 677.0166f, -669.42975f),
				new Vector3(387.78052f, 737.80646f, 566.47375f),
				new Vector3(423.8157f, 661.35266f, -212.1115f),
				new Vector3(444.6063f, 723.1206f, 360.91608f),
				new Vector3(460.0268f, 724.2298f, 482.7586f),
				new Vector3(462.5268f, 665.4831f, -601.6767f),
				new Vector3(472.0545f, 670.66797f, -332.1753f),
				new Vector3(501.59842f, 668.1235f, -467.8756f),
				new Vector3(732.08276f, 656.574f, -343.12262f)
			]
		}, {
			Territory.Hydatos, [
				new Vector3(-116.452f, 501.6467f, -331.4255f),
				new Vector3(-123.5271f, 501.02158f, -559.1261f),
				new Vector3(-253.41269f, 499.13086f, -518.068f),
				new Vector3(-256.50482f, 514.3237f, -903.4661f),
				new Vector3(-312.15518f, 502.0328f, -228.2824f),
				new Vector3(-360.7999f, 500f, -710.83484f),
				new Vector3(-365.2967f, 494f, -68.05076f),
				new Vector3(-572.7837f, 509.94162f, -836.63403f),
				new Vector3(-579.59796f, 504.07983f, -213.05739f),
				new Vector3(-583.8156f, 507.74808f, -468.509f),
				new Vector3(-611.1752f, 507.422f, -59.846287f),
				new Vector3(-712.05316f, 511.6147f, -583.89923f),
				new Vector3(-716.3769f, 504.4299f, -370.0967f),
				new Vector3(-798.5133f, 514.8021f, -754.0926f),
				new Vector3(-801.7904f, 505.8443f, -49.631298f),
				new Vector3(-852.24817f, 508.86594f, -353.7695f),
				new Vector3(-895.5052f, 507.7101f, -130.0577f),
				new Vector3(28.61431f, 496.32492f, -54.40704f),
				new Vector3(105.545f, 499.1306f, -590.0835f),
				new Vector3(113.322f, 495.31586f, -191.00049f),
				new Vector3(141.9147f, 502.4549f, -386.16882f),
				new Vector3(155.7582f, 513.09204f, -810.1353f),
				new Vector3(355.92392f, 508.80362f, -491.6639f),
				new Vector3(400.4016f, 495.38144f, -33.853767f),
				new Vector3(466.7553f, 506.69672f, -243.09f),
				new Vector3(471.341f, 506.4836f, -742.8676f),
				new Vector3(629.0432f, 500.25275f, -472.00372f),
				new Vector3(651.8131f, 500.32214f, -713.3341f),
				new Vector3(726.0835f, 514.47864f, -203.8521f),
				new Vector3(773.1991f, 495.9364f, -60.565678f),
				new Vector3(828.46814f, 512.196f, -428.00702f),
				new Vector3(873.14496f, 512.10065f, -739.3626f)
			]
		}
	};

	internal static string ToFriendlyString(this EurekaWeather weather) {
		return weather switch {
			Gales => "强风",
			Showers => "暴雨",
			FairSkies => "晴朗",
			Snow => "小雪",
			HeatWaves => "热浪",
			Thunder => "打雷",
			Blizzards => "暴雪",
			Fog => "薄雾",
			UmbralWind => "妖风",
			Thunderstorms => "雷雨",
			Gloom => "妖雾",
			_ => ""
		};
	}

	internal static string ToFriendlyString(this Territory weather) {
		return weather switch {
			Territory.Anemos => "风岛",
			Territory.Pagos => "冰岛",
			Territory.Pyros => "火岛",
			Territory.Hydatos => "水岛",
			_ => ""
		};
	}

	internal enum EurekaWeather {
		Gales,
		Showers,
		FairSkies,
		Snow,
		HeatWaves,
		Thunder,
		Blizzards,
		Fog,
		UmbralWind,
		Thunderstorms,
		Gloom,
		None
	}

	internal static readonly Dictionary<Territory, (int, EurekaWeather)[]> Weathers = new() {
		{ Territory.Anemos, [(30, FairSkies), (60, Gales), (90, Showers), (100, Snow)] },
		{ Territory.Pagos, [(10, FairSkies), (28, Fog), (46, HeatWaves), (64, Snow), (82, Thunder), (100, Blizzards)] },
		{ Territory.Pyros, [(10, FairSkies), (28, HeatWaves), (46, Thunder), (64, Blizzards), (82, UmbralWind), (100, Snow)] },
		{ Territory.Hydatos, [(12, FairSkies), (34, Showers), (56, Gloom), (78, Thunderstorms), (100, Snow)] }
	};
	internal static readonly Dictionary<Territory, Dictionary<int, string>> DeadFateDic = new() {
		{
			Territory.Anemos, new Dictionary<int, string> {
				{ 1332, "-1" }, { 1348, "-1" }, { 1333, "-1" }, { 1328, "-1" },
				{ 1344, "-1" }, { 1347, "-1" }, { 1345, "-1" }, { 1334, "-1" },
				{ 1335, "-1" }, { 1336, "-1" }, { 1339, "-1" }, { 1346, "-1" },
				{ 1343, "-1" }, { 1337, "-1" }, { 1342, "-1" }, { 1341, "-1" },
				{ 1331, "-1" }, { 1340, "-1" }, { 1338, "-1" }, { 1329, "-1" }
			}
		}, {
			Territory.Pagos, new Dictionary<int, string> {
				{ 1351, "-1" }, { 1369, "-1" }, { 1353, "-1" }, { 1354, "-1" },
				{ 1355, "-1" }, { 1366, "-1" }, { 1357, "-1" }, { 1356, "-1" },
				{ 1352, "-1" }, { 1360, "-1" }, { 1358, "-1" }, { 1361, "-1" },
				{ 1362, "-1" }, { 1359, "-1" }, { 1363, "-1" }, { 1365, "-1" },
				{ 1364, "-1" }, { 1367, "-1" }, { 1368, "-1" }
			}
		}, {
			Territory.Pyros, new Dictionary<int, string> {
				{ 1388, "-1" }, { 1389, "-1" }, { 1390, "-1" }, { 1391, "-1" },
				{ 1392, "-1" }, { 1393, "-1" }, { 1394, "-1" }, { 1395, "-1" },
				{ 1396, "-1" }, { 1397, "-1" }, { 1398, "-1" }, { 1399, "-1" },
				{ 1400, "-1" }, { 1401, "-1" }, { 1402, "-1" }, { 1403, "-1" },
				{ 1404, "-1" }, { 1407, "-1" }, { 1408, "-1" }
			}
		}, {
			Territory.Hydatos, new Dictionary<int, string> {
				{ 1412, "-1" }, { 1413, "-1" }, { 1414, "-1" }, { 1415, "-1" },
				{ 1416, "-1" }, { 1417, "-1" }, { 1418, "-1" }, { 1419, "-1" },
				{ 1420, "-1" }, { 1421, "-1" }, { 1422, "-1" }, { 1423, "-1" },
				{ 1424, "-1" }, { 1425, "-1" }
			}
		}
	};
	internal static readonly Dictionary<Territory, EurekaFate[]> XFates = new() {
		{
			Territory.Anemos, [
				new EurekaFate(1332, 1, "舞动花王——科里多仙人刺", "仙人掌", "仙人花", 6, new Vector2(13.9f, 21.6f)),
				new EurekaFate(1348, 2, "章鱼统领——常风领主", "章鱼", "海祭司", 7, new Vector2(29.7f, 27.1f)),
				new EurekaFate(1333, 3, "绝命美声——忒勒斯", "鸟", "常风哈佩亚鸟妖", 8, new Vector2(25.6f, 27.4f)),
				new EurekaFate(1328, 4, "御驾亲征——常风皇帝", "蜻蜓", "晏蜓", 9, new Vector2(17.2f, 22.2f)),
				new EurekaFate(1344, 5, "行尸走肉——卡利斯托", "熊", "瓦尔巨熊", 10, new Vector2(25.5f, 22.3f)),
				new EurekaFate(1347, 6, "无主傀儡——群偶", "群偶", "夺灵魔", 11, new Vector2(23.5f, 22.7f)),
				new EurekaFate(1345, 7, "强风妖精——哲罕南", "台风", "台风元精", 12, new Vector2(17.7f, 18.6f), Gales),
				new EurekaFate(1334, 8, "贪食者——阿米特", "暴龙", "阿卜拉克萨斯", 13, new Vector2(15f, 15.6f)),
				new EurekaFate(1335, 9, "兽脚怪人——盖因", "盖因", "追踪席兹", 14, new Vector2(13.8f, 12.5f)),
				new EurekaFate(1336, 10, "腐臭贤者——庞巴德", "举高高", "古老贪吃鬼", 15, new Vector2(28.3f, 20.4f), None, true),
				new EurekaFate(1339, 11, "幻魔蝎——塞尔凯特", "蝎子", "河道巨钳虾", 16, new Vector2(24.8f, 17.9f)),
				new EurekaFate(1346, 12, "播种者——武断魔花茱莉卡", "魔界花", "天仙子", 17, new Vector2(21.9f, 15.6f)),
				new EurekaFate(1343, 13, "胜利象征——白骑士", "白骑士", "黄昏无头骑士", 18, new Vector2(20.3f, 13f), None, true),
				new EurekaFate(1337, 14, "巨人的复仇——波吕斐摩斯", "独眼", "独眼怪", 19, new Vector2(26.4f, 14.3f)),
				new EurekaFate(1342, 15, "狂怒怪鸟——阔步西牟鸟", "阔步西牟鸟", "旧世界祖", 20, new Vector2(28.6f, 13f)),
				new EurekaFate(1341, 16, "放火大王——极其危险物质", "极其危险物质", "常风阿那罗", 21, new Vector2(35.3f, 18.3f)),
				new EurekaFate(1331, 17, "狂乱暗龙——法夫纳", "法夫纳", "龙化石", 22, new Vector2(35.5f, 21.5f), None, true),
				new EurekaFate(1340, 18, "异界魔犬——阿玛洛克", "阿玛洛克", "虚无鳞龙", 23, new Vector2(7.6f, 18.2f)),
				new EurekaFate(1338, 19, "魔王之后——拉玛什图", "拉玛什图", "瓦尔妖影", 24, new Vector2(7.7f, 23.3f), None, true),
				new EurekaFate(1329, 20, "暴风魔王——帕祖祖", "帕祖祖", "暗影幽灵", 25, new Vector2(7.4f, 21.7f), Gales, true)
			]
		}, {
			Territory.Pagos, [
				new EurekaFate(1367, 20, "雪上的幸福兔", "小兔子", "", -1, new Vector2(18, 27.5f)),
				new EurekaFate(1368, 31, "盯上宝石的幸福兔", "大兔子", "", -1, new Vector2(21, 21.5f)),
				new EurekaFate(1351, 20, "纯白的支配者——雪之女王", "周冬雨", "雪童子", 25, new Vector2(21.9f, 26.8f)),
				new EurekaFate(1369, 21, "腐烂的读书家——塔克西姆", "读书人", "珍卷恶魔", 26, new Vector2(25.4f, 27.4f), spawnByRequiredNight: true),
				new EurekaFate(1353, 22, "灰壳的鳞王——灰烬龙", "灰烬龙", "血魔", 27, new Vector2(29f, 30f)),
				new EurekaFate(1354, 23, "地壳变动之谜——异形魔虫", "魔虫", "瓦尔巨虫", 28, new Vector2(33f, 27f)),
				new EurekaFate(1355, 24, "融雪的化身——安娜波", "安娜波", "融雪元精", 29, new Vector2(33f, 21.5f), Fog),
				new EurekaFate(1366, 25, "五行眼的主人——白泽", "白泽", "啜泣百目妖", 30, new Vector2(29f, 22f)),
				new EurekaFate(1357, 26, "移动的雪洞——雪屋王", "雪屋王", "胡瓦西", 31, new Vector2(17f, 16f)),
				new EurekaFate(1356, 27, "硬质的病魔——阿萨格", "阿萨格", "徘徊欧浦肯", 32, new Vector2(10.4f, 11.4f)),
				new EurekaFate(1352, 28, "家畜的慈母——苏罗毗", "山羊", "恒冰公山羊", 33, new Vector2(10.3f, 19.5f)),
				new EurekaFate(1360, 29, "圆桌的雾王——亚瑟罗王", "螃蟹", "瓦尔利螯陆蟹", 34, new Vector2(8.7f, 15.5f), Fog),
				new EurekaFate(1358, 30, "唇亡齿寒", "双牛", "研究所弥诺陶洛斯", 35, new Vector2(14f, 19f)),
				new EurekaFate(1361, 31, "野牛的救世主——优雷卡圣牛", "圣牛", "古老水牛", 36, new Vector2(26f, 16f)),
				new EurekaFate(1362, 32, "雷云的魔兽——哈达约什", "贝爷", "虚无小龙", 37, new Vector2(30f, 19f), Thunder),
				new EurekaFate(1359, 33, "太阳的使者——荷鲁斯", "荷鲁斯", "虚无薇薇尔飞龙", 38, new Vector2(26f, 20f), HeatWaves),
				new EurekaFate(1363, 34, "暗眼王——总领安哥拉·曼纽", "大眼", "瞪视之眼", 39, new Vector2(24f, 25f)),
				new EurekaFate(1365, 35, "模仿犯——复制魔花凯西", "凯西", "阿米雷戴", 40, new Vector2(22.3f, 14.4f), Blizzards),
				new EurekaFate(1364, 35, "苍蓝冰刃——娄希", "娄希", "瓦尔腐尸", 40, new Vector2(36f, 19f), spawnByRequiredNight: true)
			]
		}, {
			Territory.Pyros, [
				new EurekaFate(1407, 35, "瞄准珊瑚的幸福兔", "小兔子", "", -1, new Vector2(24f, 26f)),
				new EurekaFate(1408, 46, "困入岩石的幸福兔", "大兔子", "", -1, new Vector2(25f, 11.1f)),
				new EurekaFate(1388, 35, "洁白的惨叫——琉科西亚", "惨叫", "涌火浮灵", 40, new Vector2(26.9f, 26.6f), spawnByRequiredNight: true),
				new EurekaFate(1389, 36, "狰狞的雷兽——佛劳洛斯", "雷兽", "雷暴元精", 41, new Vector2(30f, 28.4f), Thunder),
				new EurekaFate(1390, 37, "妖异中的辩论家——诡辩者", "诡辩者", "涌火阿班达", 42, new Vector2(31.9f, 31.3f)),
				new EurekaFate(1391, 38, "恐怖的人偶——格拉菲亚卡内", "塔塔露", "瓦尔维京人偶", 43, new Vector2(23f, 37.2f)),
				new EurekaFate(1392, 39, "图书守护者——阿斯卡拉福斯", "阿福", "过期魔导书", 44, new Vector2(19.1f, 29.1f), UmbralWind),
				new EurekaFate(1393, 40, "深渊贵族——巴钦大公爵", "大公", "暗黑行吟诗人", 45, new Vector2(17.7f, 14.5f), spawnByRequiredNight: true),
				new EurekaFate(1394, 41, "闪电的指挥者——埃托洛斯", "雷鸟", "瓦尔独爪妖禽", 46, new Vector2(10f, 14f)),
				new EurekaFate(1395, 42, "灼热的刺剑——来萨特", "蝎子", "食鸟者", 47, new Vector2(13.7f, 11.5f)),
				new EurekaFate(1396, 43, "炎热霸主——火巨人", "火巨人", "涌火陆蟹", 48, new Vector2(15.4f, 7f)),
				new EurekaFate(1397, 44, "落泪的海燕——伊丽丝", "海燕", "北境盐蓝燕", 49, new Vector2(21.3f, 11.8f)),
				new EurekaFate(1398, 45, "奇迹的生还者——佣兵雷姆普里克斯", "哥布林", "青蓝之手逃亡者", 50, new Vector2(22.1f, 8.3f)),
				new EurekaFate(1399, 46, "雷兽统领——闪电督军", "雷军", "遗弃象魔", 51, new Vector2(27.1f, 9f), Thunder),
				new EurekaFate(1400, 47, "樵夫杰科的死亡对决", "树人", "涌火树精", 52, new Vector2(29.9f, 11.8f)),
				new EurekaFate(1401, 48, "智慧与战斗之母——明眸", "明眸", "瓦尔斯卡尼特", 53, new Vector2(31.8f, 15.1f)),
				new EurekaFate(1402, 49, "相反的双子——阴·阳", "阴阳", "涌火百目妖", 54, new Vector2(11.7f, 34.3f)),
				new EurekaFate(1403, 50, "嘲讽的霜狼——斯库尔", "狼", "涌火狗灵", 55, new Vector2(24f, 30f), Blizzards),
				new EurekaFate(1404, 50, "炎蝶的女王——彭忒西勒亚", "彭女士", "瓦尔血飞蛾", 55, new Vector2(36f, 6f), HeatWaves)
			]
		}, {
			Territory.Hydatos, [
				new EurekaFate(1425, 50, "戏水的幸福兔", "兔子", "", -1, new Vector2(14.4f, 22f)),
				new EurekaFate(1412, 50, "奇怪的乌贼——卡拉墨鱼", "墨鱼", "左米特", 55, new Vector2(10.8f, 25.5f)),
				new EurekaFate(1413, 51, "暴虐的魔兽——剑齿象", "象", "丰水曙象", 56, new Vector2(9f, 17f)),
				new EurekaFate(1414, 52, "落泪的君主——摩洛", "摩洛", "瓦尔泥口花", 57, new Vector2(7.8f, 22.2f)),
				new EurekaFate(1415, 53, "惊鸿艳影——皮艾萨邪鸟", "皮鸟", "多彩冠恐鸟", 58, new Vector2(7f, 14f)),
				new EurekaFate(1416, 54, "高傲的猎人——霜鬃猎魔", "老虎", "北方猛虎", 59, new Vector2(8f, 25f)),
				new EurekaFate(1417, 55, "浴血的妖妃——达佛涅", "达芙涅", "暗黑虚无鬼鱼", 60, new Vector2(25f, 15f)),
				new EurekaFate(1418, 56, "异界的锻冶王——戈尔德马尔王", "马王", "丰水幽灵", 61, new Vector2(29f, 23.5f), spawnByRequiredNight: true),
				new EurekaFate(1419, 57, "食妖植物——琉刻", "琉刻", "虎鹰", 62, new Vector2(37f, 26f)),
				new EurekaFate(1420, 58, "业火狮子王——巴龙", "巴龙", "研究所雄狮", 63, new Vector2(32.5f, 24.5f)),
				new EurekaFate(1421, 59, "魔蛇女王——刻托", "刻托", "丰水达菲妮", 64, new Vector2(36f, 14f)),
				new EurekaFate(1423, 60, "水晶之龙——起源守望者", "守望者", "水晶爪", 65, new Vector2(32.8f, 19.7f)),
				new EurekaFate(1424, 60, "未知的威胁——未确认飞行物体", "UFO", "", -1, new Vector2(27.1f, 29f)),
				new EurekaFate(1422, 60, "兵武塔调查支援", "光灵鳐", "", -1, new Vector2(18.8f, 28.9f))
			]
		}
	};
	public static readonly Dictionary<uint, List<Vector3>> OccultBunnyPosition = new() {
		{
			1252, [
				new Vector3(283.6546f, 55.999996f, 587.3107f),
				new Vector3(-439.0463f, 115.82392f, 184.4665f),
				new Vector3(477.4074f, 96.10128f, 138.6543f),
				new Vector3(-743.601f, 96.39003f, 84.43998f),
				new Vector3(-575.6361f, 162.39511f, 668.7043f),
				new Vector3(865.0009f, 95.99958f, -214.6744f),
				new Vector3(248.9159f, 55.999996f, 791.1138f),
				new Vector3(-490.3187f, 3f, -741.0153f),
				new Vector3(720.4133f, 120f, 271.05f),
				new Vector3(466.2025f, 70.3f, 563.2519f),
				new Vector3(-701.8768f, 201f, 718.7181f),
				new Vector3(-273.0878f, 75f, 850.0336f),
				new Vector3(650.2321f, 108f, 141.1927f),
				new Vector3(827.2007f, 108f, -156.4444f),
				new Vector3(845.5334f, 98f, 777.4331f),
				new Vector3(772.3591f, 70.3f, 531.1259f),
				new Vector3(-84.73673f, 2.999999f, -796.0166f),
				new Vector3(-843.8602f, 83.657074f, -36.78173f),
				new Vector3(-727.8528f, 81.47683f, 328.9311f),
				new Vector3(-400.528f, 2.999999f, -518.3032f),
				new Vector3(-806.5123f, 107f, 887.6146f),
				new Vector3(-174.0473f, 121.00001f, 107.6488f),
				new Vector3(-771.6308f, 5f, -694.0016f),
				new Vector3(-710.266f, 3f, -451.5128f),
				new Vector3(-554.0244f, 110.698654f, -365.897f)
			]
		}
	};
}