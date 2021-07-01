﻿using System;
using System.Collections.Generic;
using System.Linq;
using Gherkin.Ast;
using SpecFlow.ExternalData.SpecFlowPlugin.Loaders;

namespace SpecFlow.ExternalData.SpecFlowPlugin.DataSource
{
    public class SpecificationProvider : ISpecificationProvider
    {
        private const string DATA_SOURCE_TAG_PREFIX = "@DataSource";
        private const string DATA_FIELD_TAG_PREFIX = "@DataField";

        private readonly IDataSourceLoaderFactory _dataSourceLoaderFactory;

        public SpecificationProvider(IDataSourceLoaderFactory dataSourceLoaderFactory)
        {
            _dataSourceLoaderFactory = dataSourceLoaderFactory;
        }

        public ExternalDataSpecification GetSpecification(IEnumerable<Tag> tags, string sourceFilePath)
        {
            var tagsArray = tags.ToArray();
            //TODO: handle multiple data source tag
            var dataSourcePath = GetTagValues(tagsArray, DATA_SOURCE_TAG_PREFIX)
                .FirstOrDefault();
            if (dataSourcePath == null)
                return null;

            var loader = _dataSourceLoaderFactory.CreateLoader();
            //TODO: get feature culture
            var dataSource = loader.LoadDataSource(dataSourcePath, sourceFilePath, null);
            var fields = GetFields(tagsArray);

            return new ExternalDataSpecification(dataSource, fields);
        }

        private Dictionary<string, string> GetFields(Tag[] tags)
        {
            var dataFieldsSettings = GetSettingValues(GetTagValues(tags, DATA_FIELD_TAG_PREFIX))
                                     .GroupBy(s => s.Key, s => s.Value)
                                     .ToArray();
            if (dataFieldsSettings.Length == 0) return null;
            return dataFieldsSettings
                .ToDictionary(
                    fs => fs.Key, 
                    fs =>
                    {
                        string value = fs.Last();
                        return string.IsNullOrEmpty(value) ? fs.Key : value; 
                    });
        }

        private IEnumerable<KeyValuePair<string, string>> GetSettingValues(IEnumerable<string> values)
        {
            return values.Select(
                v =>
                {
                    var parts = v.Split(new[] { '=' }, 2);
                    return new KeyValuePair<string, string>(parts[0], parts.Length == 2 ? parts[1] : null);
                });
        }
        
        private IEnumerable<string> GetTagValues(Tag[] tags, string prefix)
        {
            var prefixWithColon = prefix + ":";
            foreach (var tag in tags)
            {
                if (tag.Name.Equals(prefix) || tag.Name.Equals(prefixWithColon)) 
                    throw new ExternalDataPluginException($"Invalid tag '{tag.Name}'. The tag should have a value in '{prefix}:value' form.");
                
                if (tag.Name.StartsWith(prefixWithColon)) 
                    yield return tag.Name.Substring(prefixWithColon.Length);
            }
        }
    }
}
