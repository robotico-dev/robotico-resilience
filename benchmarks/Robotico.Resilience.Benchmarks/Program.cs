using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using Robotico.Resilience.Benchmarks;

ManualConfig config = ManualConfig.Create(DefaultConfig.Instance).WithOption(ConfigOptions.DisableOptimizationsValidator, true);
_ = BenchmarkRunner.Run<ResilienceBenchmarks>(config);
