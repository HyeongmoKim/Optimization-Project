#if UNITY_EDITOR

using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RedRunner.Editor.CI
{
	public static class BuildScript
	{
		private const string ApkFileName =
			"RedRunner-Development.apk";

		[MenuItem("Tools/CI/Build Android Development APK")]
		public static void BuildAndroidDevelopment()
		{
			try
			{
				ValidateAndroidBuildTarget();
				BuildAddressables();

				string[] scenes = GetEnabledScenes();
				string outputPath = GetOutputPath();

				BuildPlayerOptions options =
					new BuildPlayerOptions
					{
						scenes = scenes,
						locationPathName = outputPath,
						target = BuildTarget.Android,
						options =
							BuildOptions.Development |
							BuildOptions.AllowDebugging
					};

				EditorUserBuildSettings.buildAppBundle = false;

				Debug.Log(
					$"Android APK 빌드 시작: {outputPath}");

				BuildReport report =
					BuildPipeline.BuildPlayer(options);

				if (report.summary.result !=
					BuildResult.Succeeded)
				{
					throw new BuildFailedException(
						"Android 빌드 실패: " +
						report.summary.result);
				}

				Debug.Log(
					"Android APK 빌드 성공\n" +
					$"경로: {outputPath}\n" +
					$"크기: {report.summary.totalSize} bytes\n" +
					$"시간: {report.summary.totalTime}");

				if (Application.isBatchMode)
				{
					EditorApplication.Exit(0);
				}
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);

				if (Application.isBatchMode)
				{
					EditorApplication.Exit(1);
					return;
				}

				throw;
			}
		}

		private static void ValidateAndroidBuildTarget()
		{
			if (EditorUserBuildSettings.activeBuildTarget !=
				BuildTarget.Android)
			{
				throw new BuildFailedException(
					"활성 Build Target이 Android가 아닙니다. " +
					"Android로 Switch Platform한 뒤 다시 실행하세요.");
			}
		}

		private static void BuildAddressables()
		{
			Debug.Log("Addressables 콘텐츠 빌드 시작");

			AddressableAssetSettings.BuildPlayerContent(
				out var result);

			if (!string.IsNullOrEmpty(result.Error))
			{
				throw new BuildFailedException(
					"Addressables 빌드 실패: " +
					result.Error);
			}

			Debug.Log("Addressables 콘텐츠 빌드 성공");
		}

		private static string[] GetEnabledScenes()
		{
			string[] scenes =
				EditorBuildSettings.scenes
					.Where(scene =>
						scene.enabled &&
						!string.IsNullOrWhiteSpace(
							scene.path))
					.Select(scene => scene.path)
					.ToArray();

			if (scenes.Length == 0)
			{
				throw new BuildFailedException(
					"Build Settings에 활성화된 Scene이 없습니다.");
			}

			Debug.Log(
				"빌드 Scene:\n" +
				string.Join("\n", scenes));

			return scenes;
		}

		private static string GetOutputPath()
		{
			string projectRoot =
				Path.GetFullPath(
					Path.Combine(
						Application.dataPath,
						".."));

			string outputDirectory =
				Path.Combine(
					projectRoot,
					"Builds",
					"Android");

			Directory.CreateDirectory(outputDirectory);

			return Path.Combine(
				outputDirectory,
				ApkFileName);
		}
	}
}

#endif