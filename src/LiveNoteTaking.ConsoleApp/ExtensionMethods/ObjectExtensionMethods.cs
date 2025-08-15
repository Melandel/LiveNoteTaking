﻿using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using LiveNoteTaking.ConsoleApp.ErrorHandling.StringRepresentation;

namespace LiveNoteTaking.ConsoleApp.ExtensionMethods;

public static class ObjectExtensionMethods
{
	static readonly JsonSerializerOptions SerializerOptions;
	static ObjectExtensionMethods()
	{
		SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
		SerializerOptions.Converters.Add(new ValueObjectExposedViaUserDefinedConversionJsonConverter());
		SerializerOptions.Converters.Add(new FirstClassCollectionJsonConverter());
	}

	public static string GetStringRepresentation(this object? obj)
	=> obj switch
	{
		null => "null",
		_ => JsonSerializer.Serialize(obj, SerializerOptions)
	};

	internal static bool IsFirstClassCollectionWithKnownCount(this object? obj, out int count)
	{
		if (obj == null)
		{
			count = 0;
			return false;
		}
		return obj.DefinesAFirstClassCollectionWithKnownCount(out count);
	}

	internal static bool DefinesAFirstClassCollectionWithKnownCount(this object obj, out int count)
	{
		var type = obj.GetType();
		if (!type.IsDefinedByOurOrganization())
		{
			count = 0;
			return false;
		}

		if (!type.DefinesConstructorWhoseParametersAllHaveAMatchingField(out var fields, out _))
		{
			count = 0;
			return false;
		}

		var encapsulatedCollectionsFields = fields.Where(field => field.FieldType.IsEnumerableWithoutBeingString());
		if (encapsulatedCollectionsFields.Count() != 1)
		{
			count = 0;
			return false;
		}

		var encapsulatedCollectionField = encapsulatedCollectionsFields.First();
		try
		{
			var encapsulatedCollection = encapsulatedCollectionField.GetValue(obj);
			if (encapsulatedCollection == null)
			{
				count = 0;
				return false;
			}

			if (encapsulatedCollection.IsReadOnlyCollectionWithoutBeingString(out var asReadOnlyCollection))
			{
				count = asReadOnlyCollection.Count;
				return true;
			}

			if (encapsulatedCollection.DefinesAFirstClassCollectionWithKnownCount(out var size))
			{
				count = size;
				return true;
			}

			count = 0;
			return false;
		}
		catch
		{
			count = 0;
			return false;
		}
	}

	internal static bool IsEnumerableWithoutBeingString(this object obj, out IEnumerable<object> asEnumerable)
	{
		if (obj.GetType().IsEnumerableWithoutBeingString())
		{
			asEnumerable = ((System.Collections.IEnumerable)obj).Cast<object>();
			return true;
		}

		asEnumerable = Array.Empty<object>();
		return false;
	}

	internal static bool IsReadOnlyCollectionWithoutBeingString(this object? obj, out IReadOnlyCollection<object> asReadOnlyCollection)
	{
		if (obj == null)
		{
			asReadOnlyCollection = Array.Empty<object>();
			return false;
		}

		if (obj.GetType().IsReadOnlyCollectionWithoutBeingString())
		{
			asReadOnlyCollection = ((System.Collections.IEnumerable)obj).Cast<object>().ToArray();
			return true;
		}

		asReadOnlyCollection = Array.Empty<object>();
		return false;
	}

	public static bool HasUserDefinedConversions(this object obj, out MethodInfo[] converters)
	{
		converters = obj.GetType().GetUserDefinedConversions();
		return converters.Any();
	}
}

