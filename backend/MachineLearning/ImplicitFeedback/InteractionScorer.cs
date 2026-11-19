
using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning.ContentBased;

namespace Palette.Api.MachineLearning.ImplicitFeedback;

public sealed class InteractionScorer
{
    private readonly InteractionWeights _weights;

    // Creates an interaction scorer with validated weights.
    public InteractionScorer(InteractionWeights? weights = null)
    {
        _weights = weights ?? new InteractionWeights();

        ValidateWeights(_weights);
    }

    // Returns the configured weight for an interaction type.
    public float GetWeight(InteractionType type)
    {
        return type switch
        {
            InteractionType.Search => _weights.Search,

            InteractionType.SearchSelection =>
                _weights.SearchSelection,

            InteractionType.Stored => _weights.Stored,

            InteractionType.Completed => _weights.Completed,

            InteractionType.PositiveReview =>
                _weights.PositiveReview,

            _ => throw new ArgumentOutOfRangeException(
                nameof(type), type, "Unknown interaction type.")
        };
    }

    // Sums each user's interactions once per media and type, omitting other users
    // and interaction types with zero weight.
    public IReadOnlyDictionary<Guid, float> Score(
        Guid userId,
        IReadOnlyList<UserInteraction> interactions)
    {
        ArgumentNullException.ThrowIfNull(interactions);

        var mediaScores = new Dictionary<Guid, float>();

        var processedInteractions =
            new HashSet<(Guid MediaId, InteractionType Type)>();

        foreach (var interaction in interactions)
        {
            ArgumentNullException.ThrowIfNull(interaction);

            if (interaction.UserId != userId)
            {
                continue;
            }

            float weight = GetWeight(interaction.Type);

            var key = (interaction.MediaId, interaction.Type);

            if (!processedInteractions.Add(key))
            {
                continue;
            }

            if (weight == 0f)
            {
                continue;
            }

            float updatedScore =
                mediaScores.GetValueOrDefault(
                    interaction.MediaId) + weight;

            if (!float.IsFinite(updatedScore))
            {
                throw new OverflowException(
                    "Accumulated interaction weight is too large.");
            }

            mediaScores[interaction.MediaId] = updatedScore;
        }

        return new System.Collections.ObjectModel
            .ReadOnlyDictionary<Guid, float>(mediaScores);
    }

    // Attaches media features to scored interactions when the features are available.
    public IReadOnlyList<WeightedMediaInteraction>
        BuildWeightedInteractions(
            Guid userId,
            IReadOnlyList<UserInteraction> interactions,
            IReadOnlyDictionary<Guid, MediaFeatures> mediaFeatures)
    {
        ArgumentNullException.ThrowIfNull(mediaFeatures);

        var scores = Score(userId, interactions);

        var result = new List<WeightedMediaInteraction>();

        foreach (var entry in scores)
        {
            if (!mediaFeatures.TryGetValue(
                entry.Key, out var media))
            {
                continue;
            }

            ArgumentNullException.ThrowIfNull(media);

            if (media.MediaId != entry.Key)
            {
                throw new ArgumentException(
                    "Media identifiers must match dictionary keys.",
                    nameof(mediaFeatures));
            }

            result.Add(new WeightedMediaInteraction(
                media,
                entry.Value));
        }

        return result.AsReadOnly();
    }

    // Rejects invalid weights and configurations with no positive weight.
    private static void ValidateWeights(
        InteractionWeights weights)
    {
        float[] values =
        {
            weights.Search,
            weights.SearchSelection,
            weights.Stored,
            weights.Completed,
            weights.PositiveReview
        };

        foreach (float weight in values)
        {
            if (!float.IsFinite(weight) || weight < 0f)
            {
                throw new ArgumentException(
                    "Interaction weights must be finite and non-negative.",
                    nameof(weights));
            }
        }

        if (!values.Any(weight => weight > 0f))
        {
            throw new ArgumentException(
                "At least one interaction weight must be positive.",
                nameof(weights));
        }
    }
}
