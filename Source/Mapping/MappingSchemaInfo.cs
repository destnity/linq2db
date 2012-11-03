﻿using System;
using System.Collections.Concurrent;

namespace LinqToDB.Mapping
{
	using Common;
	using Metadata;

	class MappingSchemaInfo
	{
		public MappingSchemaInfo(string configuration)
		{
			Configuration = configuration;
		}

		public string          Configuration;
		public IMetadataReader MetadataReader;

		volatile ConcurrentDictionary<Type,object> _defaultValues;

		public Option GetDefaultValue(Type type)
		{
			if (_defaultValues == null)
				return Option.None;

			object o;
			_defaultValues.TryGetValue(type, out o);
			return Option.Some(o);
		}

		public void SetDefaultValue(Type type, object value)
		{
			if (_defaultValues == null)
				lock (this)
					if (_defaultValues == null)
						_defaultValues = new ConcurrentDictionary<Type,object>();

			_defaultValues[type] = value;
		}

		ConvertInfo _convertInfo;

		public void SetConvertInfo(Type from, Type to, ConvertInfo.LambdaInfo expr)
		{
			if (_convertInfo == null)
				_convertInfo = new ConvertInfo();
			_convertInfo.Set(from, to, expr);
		}

		public ConvertInfo.LambdaInfo GetConvertInfo(MappingSchema mappingSchema, Type from, Type to)
		{
			return _convertInfo == null ? null : _convertInfo.Get(mappingSchema, @from, to, false);
		}
	}
}