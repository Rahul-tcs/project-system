﻿// Licensed to the .NET Foundation under one or more agreements. The .NET Foundation licenses this file to you under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using EnumCollection = System.Collections.Generic.ICollection<Microsoft.VisualStudio.ProjectSystem.Properties.IEnumValue>;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Frameworks
{
    /// <summary>
    ///     Responsible for producing valid values for the TargetFramework property from evaluation.
    /// </summary>
    [ExportDynamicEnumValuesProvider("SupportedTargetFrameworksEnumProvider")]
    [AppliesTo(ProjectCapability.DotNet)]
    internal class SupportedTargetFrameworksEnumProvider : SupportedValuesProvider
    {
        protected override string[] RuleNames => new [] { SupportedNETCoreAppTargetFramework.SchemaName, SupportedNETFrameworkTargetFramework.SchemaName, SupportedNETStandardTargetFramework.SchemaName, ConfigurationGeneral.SchemaName };

        protected ProjectProperties Properties;

        [ImportingConstructor]
        public SupportedTargetFrameworksEnumProvider(
            ConfiguredProject project,
            IProjectSubscriptionService subscriptionService,
            ProjectProperties properties)
            : base(project, subscriptionService) {
            Properties = properties;
        }

        protected override EnumCollection Transform(IProjectSubscriptionUpdate input)
        {
            IProjectRuleSnapshot? configurationgeneral = input.CurrentState[ConfigurationGeneral.SchemaName];
            
            string? targetframeworkidentifier = configurationgeneral.Properties[ConfigurationGeneral.TargetFrameworkIdentifierProperty];

            string ruleName;

            if (StringComparers.FrameworkIdentifiers.Equals(targetframeworkidentifier, TargetFrameworkIdentifiers.NetCoreApp))
            {
                ruleName = SupportedNETCoreAppTargetFramework.SchemaName;
            }
            else if (StringComparers.FrameworkIdentifiers.Equals(targetframeworkidentifier, TargetFrameworkIdentifiers.NetFramework))
            {
                ruleName = SupportedNETFrameworkTargetFramework.SchemaName;
            }
            else if (StringComparers.FrameworkIdentifiers.Equals(targetframeworkidentifier, TargetFrameworkIdentifiers.NetStandard))
            {
                ruleName = SupportedNETStandardTargetFramework.SchemaName;
            }
            else
            {
                string? targetframework = configurationgeneral.Properties[ConfigurationGeneral.TargetFrameworkProperty];

                var returnList = new List<IEnumValue>();

                // This is the case where the TargetFrameworkProperty has a user-defined value.
                if (!Strings.IsNullOrEmpty(targetframework))
                {
                    returnList.Add(new PageEnumValue(new EnumValue
                    {
                        Name = targetframework,
                        DisplayName = targetframework
                    }));
                }
                
                return returnList;
            }

            IProjectRuleSnapshot snapshot = input.CurrentState[ruleName];

            int capacity = snapshot.Items.Count;
            var list = new List<IEnumValue>(capacity);

            list.AddRange(snapshot.Items.Select(ToEnumValue));
            list.Sort(SortValues); // TODO: This is a hotfix for item ordering. Remove this when completing: https://github.com/dotnet/project-system/issues/7025
            return list;
        }

        protected override IEnumValue ToEnumValue(KeyValuePair<string, IImmutableDictionary<string, string>> item)
        {
            return new PageEnumValue(new EnumValue()
            {
                // Example: <SupportedTargetFramework  Include=".NETCoreApp,Version=v5.0"
                //                                     DisplayName=".NET 5.0" />

                Name = item.Key,
                DisplayName = item.Value[SupportedTargetFramework.DisplayNameProperty],
            });
        }

        protected override int SortValues(IEnumValue a, IEnumValue b)
        {
            return NaturalStringComparer.Instance.Compare(a.DisplayName, b.DisplayName);
        }
    }
}
