using System;

namespace CloudCafe.Coffee
{
    public enum EspressoRoast { Light, Medium, Dark }

    public readonly struct EspressoBeanProfile
    {
        public string BeanId { get; }
        public EspressoRoast Roast { get; }
        public string RoastKo { get; }
        public float RoastExtractionFactor { get; }
        public int GrindSetting { get; }
        public float TemperatureCelsius { get; }
        public float BrewRatio { get; }
        public string GrindReasonKo { get; }
        public string TemperatureReasonKo { get; }
        public string RatioReasonKo { get; }
        internal string FlavorCueKo { get; }

        internal EspressoBeanProfile(string beanId, EspressoRoast roast, string roastKo, float roastExtractionFactor,
            int grindSetting, float temperatureCelsius, float brewRatio, string flavorCueKo, string grindReasonKo,
            string temperatureReasonKo, string ratioReasonKo)
        {
            BeanId = beanId;
            Roast = roast;
            RoastKo = roastKo;
            RoastExtractionFactor = roastExtractionFactor;
            GrindSetting = grindSetting;
            TemperatureCelsius = temperatureCelsius;
            BrewRatio = brewRatio;
            FlavorCueKo = flavorCueKo;
            GrindReasonKo = grindReasonKo;
            TemperatureReasonKo = temperatureReasonKo;
            RatioReasonKo = ratioReasonKo;
        }
    }

    public static class EspressoBeanProfiles
    {
        private static readonly EspressoBeanProfile DefaultProfile = new EspressoBeanProfile(
            null, EspressoRoast.Medium, null, 1f, 5, 93f, 2f, "원두 고유의 향",
            "중간 분쇄도로 시작해 물이 통과하는 속도를 확인해요.",
            "중간 온도에서 시작해 신맛과 쓴맛의 균형을 살펴요.",
            "원두의 두 배 무게를 받아 농도와 향을 확인해요.");

        // These are starting recipes for the catalog's depicted lots. Only roast affects the
        // solubility approximation. The per-lot settings are calibrated around tasting goals.
        private static readonly EspressoBeanProfile[] Profiles =
        {
            MediumLight("batian", 4, 94f, 2.20f, "청포도와 홍차",
                "한 단계 곱게 갈아 물이 너무 빨리 통과하지 않도록 해요.",
                "물을 조금 더 뜨겁게 써서 청포도와 홍차 향에 단맛을 더할 출발점이에요.",
                "조금 길게 받아 시트러스의 선명함과 부드러운 농도를 함께 살펴요."),
            MediumLight("bourbon", 5, 94f, 2.10f, "붉은 베리와 향신료",
                "중간 분쇄로 붉은 베리와 향신료가 균형을 이루는지 살펴요.",
                "미디엄 라이트에 열을 조금 더 주어 단맛까지 끌어내는 출발점이에요.",
                "1:2보다 살짝 길게 받아 붉은 베리의 산미가 부드러워지는지 확인해요."),
            Medium("catuai", 6, 92f, 1.75f, "밀크 초콜릿과 캐러멜",
                "한 단계 굵게 갈아 농도가 짙은 샷에서도 물길이 지나치게 느려지지 않게 해요.",
                "비교적 잘 녹는 미디엄 로스트는 낮은 온도부터 맛을 확인해요.",
                "짧게 받아 밀크 초콜릿과 캐러멜의 짙고 부드러운 질감을 살려요."),
            MediumLight("caturra", 5, 93.5f, 2.00f, "붉은 베리와 초콜릿",
                "중간 분쇄로 시작해 크림 같은 질감과 흐름의 균형을 봐요.",
                "미디엄 라이트에 온도를 약간 높여 베리 산미와 초콜릿 단맛을 함께 살펴요.",
                "1:2로 받아 붉은 베리와 초콜릿의 균형을 기준으로 삼아요."),
            MediumLight("gesha", 5, 94f, 2.35f, "재스민과 복숭아",
                "중간 분쇄로 길게 받으면서도 물길이 지나치게 느려지지 않도록 해요.",
                "미디엄 라이트에 열을 조금 더 주어 복숭아의 단맛을 함께 살펴요.",
                "길게 받아 농도를 낮추고 재스민과 베르가모트의 섬세한 향을 살펴요."),
            MediumLight("kilimanjaro_bourbon", 5, 94f, 2.00f, "홍차와 코코아",
                "중간 분쇄로 코코아의 바디를 유지할 수 있는 흐름부터 맞춰요.",
                "미디엄 라이트에서 시트러스와 코코아가 함께 느껴지는지 높은 쪽부터 살펴요.",
                "1:2로 받아 홍차 같은 향과 코코아의 질감 사이에서 균형을 잡아요."),
            MediumLight("maragogipe", 5, 93.5f, 2.15f, "들꽃과 오렌지",
                "중간 분쇄로 들꽃과 초콜릿이 함께 드러나는 흐름을 찾아요.",
                "온도를 약간 높여 오렌지 산미 뒤에 초콜릿 단맛이 나오는지 봐요.",
                "조금 길게 받아 들꽃과 오렌지의 향을 살펴보기 좋은 농도로 맞춰요."),
            MediumLight("marsellesa", 5, 93f, 1.95f, "흑설탕과 다크 초콜릿",
                "중간 분쇄로 진한 맛을 담되 물길이 막히지 않는 출발점을 잡아요.",
                "중간 온도에서 시작해 다크 초콜릿의 쌉쌀함과 흑설탕 단맛을 살펴요.",
                "1:2보다 살짝 짧게 받아 흑설탕과 초콜릿의 질감을 또렷하게 남겨요."),
            Medium("mundo_novo", 6, 92.5f, 1.85f, "코코아와 구운 견과",
                "한 단계 굵게 갈아 짧은 샷에서도 유량을 확보해요.",
                "미디엄 로스트는 비교적 잘 녹아 낮은 온도에서 코코아의 쌉쌀함을 살펴요.",
                "조금 짧게 받아 코코아와 구운 견과의 묵직한 질감을 살려요."),
            MediumLight("pacamara", 4, 94.5f, 2.15f, "자두와 청포도",
                "한 단계 곱게 갈아 자두와 청포도의 산미에 어울릴 단맛을 충분히 받아봐요.",
                "미디엄 라이트에 온도를 높여 흑설탕 같은 단맛을 끌어낼 출발점으로 삼아요.",
                "조금 길게 받아 자두와 청포도 향을 분명하게 느낄 농도를 찾아요."),
            MediumLight("pache", 5, 93.5f, 2.05f, "붉은 사과와 캐러멜",
                "중간 분쇄로 붉은 사과와 코코아의 균형부터 맞춰요.",
                "온도를 약간 높여 사과의 산미 뒤에 캐러멜 단맛이 이어지는지 살펴요.",
                "1:2보다 살짝 길게 받아 사과 향을 살피면서 코코아의 바디를 남겨요."),
            MediumLight("parainema", 5, 94f, 2.20f, "열대과일과 캐러멜",
                "중간 분쇄에서 물길을 안정시켜 열대과일과 캐러멜의 균형을 찾아요.",
                "온도를 높여 미디엄 라이트의 단맛을 더 끌어낼 출발점으로 삼아요.",
                "조금 길게 받아 시트러스의 선명함과 캐러멜 여운을 함께 살펴요."),
            MediumLight("ruiru_11", 4, 94f, 2.15f, "블랙커런트와 꽃",
                "한 단계 곱게 갈아 블랙커런트 산미 뒤의 단맛까지 살펴봐요.",
                "미디엄 라이트에 열을 조금 더 주어 꽃향 뒤로 단맛이 이어지는지 확인해요.",
                "조금 길게 받아 블랙커런트의 밀도를 낮추고 꽃향을 함께 살펴요."),
            MediumLight("sl28", 4, 95f, 2.30f, "블랙커런트와 홍차",
                "한 단계 곱게 갈아 물이 너무 빨리 지나가며 신맛만 도드라지는지 살펴요.",
                "로스팅이 비교적 밝아 높은 온도에서 단맛과 산미의 균형을 확인해요.",
                "길게 받아 짙은 블랙커런트의 농도를 낮추고 홍차 같은 여운을 살펴요."),
            MediumLight("sl34", 5, 94f, 2.05f, "블랙베리와 흑설탕",
                "중간 분쇄로 적포도의 산미와 흑설탕의 진한 질감을 맞춰요.",
                "온도를 조금 높여 미디엄 라이트의 과일 향 뒤에 단맛이 이어지는지 살펴요.",
                "1:2보다 살짝 길게 받아 블랙베리 향과 흑설탕의 바디를 함께 살려요."),
            MediumLight("typica", 5, 93.5f, 2.10f, "핵과일과 초콜릿",
                "중간 분쇄로 핵과일과 초콜릿이 같이 드러나는 흐름부터 맞춰요.",
                "온도를 조금 높여 시트러스 산미와 초콜릿 단맛을 함께 살펴요.",
                "조금 길게 받아 핵과일 향이 초콜릿의 질감에 묻히지 않는지 확인해요."),
            MediumLight("villa_sarchi", 5, 93f, 2.05f, "살구와 꿀",
                "중간 분쇄로 꿀과 살구의 단맛이 산미와 어우러지는 흐름부터 맞춰요.",
                "중간 온도로 시작해 시트러스보다 꿀과 살구의 단맛이 잘 느껴지는지 살펴요.",
                "1:2보다 살짝 길게 받아 살구 향을 열어 주면서 꿀 같은 질감을 남겨요."),
            MediumLight("yirgacheffe_kurume", 4, 94.5f, 2.25f, "홍차와 핵과일",
                "한 단계 곱게 갈아 물과 만나는 면적을 늘리고 단맛을 충분히 받아봐요.",
                "미디엄 라이트에 열을 더 주어 시트러스와 핵과일의 단맛을 함께 살펴요.",
                "조금 길게 받아 농도를 낮추고 홍차와 핵과일의 섬세한 향을 살펴요.")
        };

        public static EspressoBeanProfile ForBean(string beanId)
        {
            foreach (EspressoBeanProfile profile in Profiles)
            {
                if (string.Equals(profile.BeanId, beanId, StringComparison.Ordinal)) return profile;
            }
            return DefaultProfile;
        }

        public static EspressoBeanProfile ForBean(string beanId, EspressoRoast roast)
        {
            EspressoBeanProfile reference = ForBean(beanId);
            if (reference.BeanId == null) return reference;
            // Medium is the center of the range. The catalog's medium-light products start
            // slightly hotter and longer, so shift those reference recipes back first.
            bool lightReference = reference.Roast == EspressoRoast.Light;
            int mediumGrind = Math.Min(reference.GrindSetting + 1, 6);
            float mediumTemperature = reference.TemperatureCelsius - (lightReference ? 1f : 0f);
            float mediumRatio = reference.BrewRatio - (lightReference ? .10f : 0f);
            string flavorCue = reference.FlavorCueKo;
            switch (roast)
            {
                case EspressoRoast.Light:
                    return WithRoast(reference, EspressoRoast.Light, .94f, mediumGrind - 1,
                        mediumTemperature + 1.5f, mediumRatio + .20f,
                        "밝게 볶은 원두는 곱게 갈아 물과 닿는 면적을 늘려요.",
                        "라이트 로스트에서 성분이 충분히 녹도록 온도를 높여요.",
                        $"길게 받아 농도를 낮추고 {flavorCue} 같은 향미를 살펴요.");
                case EspressoRoast.Dark:
                    return WithRoast(reference, EspressoRoast.Dark, 1.06f, mediumGrind + 1,
                        mediumTemperature - 1.5f, mediumRatio - .20f,
                        "잘 녹는 다크 로스트는 굵게 갈아 물이 빠르게 지나가도록 해요.",
                        "낮은 온도로 시작해 볶은 향과 쌉쌀함의 균형을 살펴요.",
                        "짧게 받아 짙은 질감을 유지하고 볶은 맛의 여운을 확인해요.");
                default:
                    return WithRoast(reference, EspressoRoast.Medium, 1f, mediumGrind,
                        mediumTemperature, mediumRatio,
                        "중간 분쇄도로 유량과 퍽 저항의 균형부터 맞춰요.",
                        "중간 온도에서 원두의 산미와 로스팅으로 생긴 단맛을 살펴요.",
                        $"중간 비율에서 {flavorCue} 같은 향미와 질감의 균형을 확인해요.");
            }
        }

        public static string RoastNameKo(EspressoRoast roast)
        {
            switch (roast)
            {
                case EspressoRoast.Light: return "라이트";
                case EspressoRoast.Dark: return "다크";
                default: return "미디엄";
            }
        }

        private static EspressoBeanProfile WithRoast(EspressoBeanProfile reference, EspressoRoast roast,
            float extractionFactor, int grind, float temperature, float ratio, string grindReason,
            string temperatureReason, string ratioReason) =>
            new EspressoBeanProfile(reference.BeanId, roast, RoastNameKo(roast), extractionFactor, grind, temperature,
                (float)Math.Round(ratio, 2), reference.FlavorCueKo, grindReason, temperatureReason, ratioReason);

        private static EspressoBeanProfile MediumLight(string beanId, int grindSetting, float temperature, float ratio,
            string flavorCue, string grindReason, string temperatureReason, string ratioReason) =>
            new EspressoBeanProfile(beanId, EspressoRoast.Light, "미디엄 라이트", .97f, grindSetting, temperature, ratio,
                flavorCue, grindReason, temperatureReason, ratioReason);

        private static EspressoBeanProfile Medium(string beanId, int grindSetting, float temperature, float ratio,
            string flavorCue, string grindReason, string temperatureReason, string ratioReason) =>
            new EspressoBeanProfile(beanId, EspressoRoast.Medium, RoastNameKo(EspressoRoast.Medium), 1f, grindSetting,
                temperature, ratio, flavorCue, grindReason, temperatureReason, ratioReason);
    }
}
