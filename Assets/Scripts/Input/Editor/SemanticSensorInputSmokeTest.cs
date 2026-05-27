#if UNITY_EDITOR
using InformationString.Input;
using UnityEditor;
using UnityEngine;

namespace InformationString.Input.Editor
{
    public static class SemanticSensorInputSmokeTest
    {
        [MenuItem("InformationString/Run Semantic Parser Smoke Test")]
        public static void RunParserSmokeTest()
        {
            Test4SlotFrame();
            Test9SlotFrame();
            TestEmptyLinks();
            Debug.Log("[SemanticSensorInputSmokeTest] All parser checks passed.");
        }

        private static void Test4SlotFrame()
        {
            const string json =
                "{\"info\":[\"Info1\",\"Info2\",\"empty\",\"empty\"]," +
                "\"rhythm\":[\"Rhythm1\",\"Rhythm2\",\"empty\",\"empty\"]," +
                "\"links\":[{\"info\":\"Info1\",\"rhythmSlot\":0},{\"info\":\"Info2\",\"rhythmSlot\":1}]}";

            if (!SerialSensorInput.TryParseFrame(json, 4, out var frame))
                throw new System.InvalidOperationException("4-slot frame parse failed.");

            Assert(frame.InfoSlots.Length == 4, "info length");
            Assert(frame.InfoSlots[0] == "Info1", "info[0]");
            Assert(frame.RhythmSlots[2] == "empty", "rhythm[2]");
            Assert(frame.Links.Length == 2, "links length");
            Assert(frame.Links[1].RhythmSlot == 1, "link rhythmSlot");
        }

        private static void Test9SlotFrame()
        {
            var json =
                "{\"info\":[\"Info1\",\"empty\",\"empty\",\"empty\",\"empty\",\"empty\",\"empty\",\"empty\",\"empty\"]," +
                "\"rhythm\":[\"Rhythm1\",\"empty\",\"empty\",\"empty\",\"empty\",\"empty\",\"empty\",\"empty\",\"empty\"]," +
                "\"links\":[{\"info\":\"Info1\",\"rhythmSlot\":0}]}";

            if (!SerialSensorInput.TryParseFrame(json, 9, out var frame))
                throw new System.InvalidOperationException("9-slot frame parse failed.");

            Assert(frame.InfoSlots.Length == 9, "info length 9");
            Assert(frame.Links[0].InfoId == "Info1", "link info");
        }

        private static void TestEmptyLinks()
        {
            const string json =
                "{\"info\":[\"empty\",\"empty\",\"empty\",\"empty\"]," +
                "\"rhythm\":[\"empty\",\"empty\",\"empty\",\"empty\"]," +
                "\"links\":[]}";

            if (!SerialSensorInput.TryParseFrame(json, 4, out var frame))
                throw new System.InvalidOperationException("empty links parse failed.");

            Assert(frame.Links.Length == 0, "empty links");
        }

        private static void Assert(bool condition, string label)
        {
            if (!condition)
                throw new System.InvalidOperationException($"Assertion failed: {label}");
        }
    }
}
#endif
