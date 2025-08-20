using System.Diagnostics.CodeAnalysis;
using Credo.Core.Minio.Models;
using Minio.DataModel.ILM;
using Minio.DataModel.Tags;

namespace Credo.Core.Minio;

public static class MinioLifecycleRules
{
    internal static LifecycleConfiguration ToLifecycleConfiguration(this StoringPolicy storingPolicy)
    {
        var configToMerge = new LifecycleConfiguration
        {
            Rules = []
        };
        var transitionDurationDefinition = $"{storingPolicy.TransitionAfterDays}Days";
        var transRuleId = $"transition-{transitionDurationDefinition}";
        LifecycleRule lcr = new()
        {
            ID = transRuleId,
            Filter = new RuleFilter
            {
                Tag = new Tagging(new Dictionary<string, string>() { { transRuleId, transitionDurationDefinition } }, true)
            },
            TransitionObject = new Transition()
            {
                StorageClass = "COLD-TIER",
                Days = storingPolicy.TransitionAfterDays
            }
        };

        configToMerge.Rules.Add(lcr);

        if (storingPolicy.ExpirationAfterDays is not null)
        {
            var expirationDurationDefinition = $"{storingPolicy.ExpirationAfterDays}Days";
            var expRuleId = $"expire-{expirationDurationDefinition}";
            configToMerge.Rules.Add(new()
            {
                ID = expRuleId,
                Filter = new RuleFilter
                {
                    Tag = new Tagging(new Dictionary<string, string>() { { transRuleId, transitionDurationDefinition } }, true)
                },
                Expiration = new Expiration()
                {
                    Days = storingPolicy.ExpirationAfterDays
                }
            });
        }

        return configToMerge;
    }

    internal static Tagging SelectTags(this LifecycleConfiguration lc)
    {
        var tags = lc.Rules.Select(x => x.Filter.Tag).SelectMany(x => x.Tags);
        var dict = tags.ToDictionary(tag => tag.Key, tag => tag.Value);

        return new Tagging(dict, true);
    }

    internal static Tagging SelectTags(this StoringPolicy storingPolicy)
    {
        var lc = storingPolicy.ToLifecycleConfiguration();
        var tagging = new Tagging();
        var tags = lc.Rules.Select(x => x.Filter.Tag).SelectMany(x => x.Tags);
        foreach (var tag in tags)
        {
            tagging.Tags.Add(tag);
        }

        return tagging;
    }


    public static LifecycleConfiguration DefaultLifeCycleConfiguration() => new(
        new List<LifecycleRule>
        {
            new()
            {
                ID = "default",
                Filter = new RuleFilter
                {
                    Tag = new Tagging(new Dictionary<string, string>() { { "default", "90days" } }, true)
                },
                TransitionObject = new Transition()
                {
                    StorageClass = "COLD-TIER",
                    Days = 90
                }
            }
        });

    [SuppressMessage("ReSharper", "SimplifyLinqExpressionUseAll")]
    public static LifecycleConfiguration MergeLifeCycleConfiguration(LifecycleConfiguration? configToMerge, StoringPolicy? storingPolicy)
    {
        if (storingPolicy is null)
        {
            return DefaultLifeCycleConfiguration();
        }

        configToMerge ??= new LifecycleConfiguration();

        var transitionDurationDefinition = $"{storingPolicy.TransitionAfterDays}Days";
        var transRuleId = $"transition-{transitionDurationDefinition}";
        if (!configToMerge.Rules.Any(x => x.ID == transRuleId))
        {
            configToMerge.Rules.Add(new()
            {
                ID = transRuleId,
                Filter = new RuleFilter
                {
                    Tag = new Tagging
                    {
                        Tags = { new KeyValuePair<string, string>(transRuleId, transitionDurationDefinition) }
                    }
                },
                TransitionObject = new Transition()
                {
                    StorageClass = "COLD-TIER",
                    Days = storingPolicy.TransitionAfterDays
                }
            });
        }

        if (storingPolicy.ExpirationAfterDays is not null)
        {
            var expirationDurationDefinition = $"{storingPolicy.ExpirationAfterDays}Days";
            var expRuleId = $"expire-{expirationDurationDefinition}";
            if (!configToMerge.Rules.Any(x => x.ID == expRuleId))
            {
                configToMerge.Rules.Add(new()
                {
                    ID = expRuleId,
                    Filter = new RuleFilter
                    {
                        Tag = new Tagging
                        {
                            Tags = { new KeyValuePair<string, string>(expRuleId, expirationDurationDefinition) }
                        }
                    },
                    Expiration = new Expiration()
                    {
                        Days = storingPolicy.ExpirationAfterDays
                    }
                });
            }
        }

        return configToMerge;
    }

    //new(
    //new List<LifecycleRule>
    //{
    //    new()
    //    {
    //        ID = "default",
    //        Filter = new RuleFilter
    //        {
    //            Tag = new Tagging
    //            {
    //                Tags = { new KeyValuePair<string, string>("default", "90days") }
    //            }
    //        },
    //        TransitionObject = new Transition()
    //        {
    //            StorageClass = "COLD-TIER",
    //            Days = 90
    //        }
    //    }
    //});

    public static LifecycleRule WithAdditionalRules() => new();
}

class MyClass
{
}