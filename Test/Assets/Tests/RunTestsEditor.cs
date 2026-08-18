#if UNITY_INCLUDE_TESTS
using UnityEditor;
using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;

namespace Game.Tests
{
    public class RunTestsEditor
    {
        [MenuItem("Tools/Run All Tests")]
        public static void RunAll()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var filter = new Filter()
            {
                testMode = TestMode.EditMode
            };
            var settings = new ExecutionSettings(filter);

            var callback = new TestCallback();
            api.RegisterCallbacks(callback);
            api.Execute(settings);
            Debug.Log("=== Test execution started ===");
        }
    }

    public class TestCallback : IErrorCallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log($"RunStarted: {testsToRun.Name}");
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            Debug.Log($"=== TEST RESULTS ===");
            Debug.Log($"Passed: {result.PassCount}, Failed: {result.FailCount}");
            LogFailures(result);
        }

        private void LogFailures(ITestResultAdaptor result)
        {
            if (result.ResultState == "Failed" && result.Test.TestCaseCount > 0)
            {
                Debug.LogError($"  FAIL: {result.Name} - {result.Message}");
            }
            foreach (var child in result.Children)
            {
                LogFailures(child);
            }
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test.TestCaseCount > 0)
            {
                string status = result.ResultState == "Passed" ? "PASS" : result.ResultState;
                Debug.Log($"  {status}: {result.Name}");
            }
        }

        public void OnError(string message)
        {
            Debug.LogError($"Test Runner Error: {message}");
        }
    }
}
#endif
