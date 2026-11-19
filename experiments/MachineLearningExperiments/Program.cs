using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Data;
using Palette.Api.MachineLearning.Evaluation;

var ratings = DataGenerator.Generate(seed: 42);
var split = DatasetSplitter.Split(ratings);

DatasetAnalyzer.Analyze(split.Training);