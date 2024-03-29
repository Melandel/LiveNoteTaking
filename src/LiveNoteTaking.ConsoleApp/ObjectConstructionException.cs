using System.Text;
using System.Text.Json;

public class ObjectConstructionException : Exception
{
	readonly Type _typeOfObjectUnderConstruction;
	ObjectConstructionException(Type typeOfObjectUnderConstruction, string message, Exception? innerException = null)
	: base(message, innerException)
	{
		_typeOfObjectUnderConstruction = typeOfObjectUnderConstruction;
	}

	public static ObjectConstructionException FromInvalidMemberValue(
		Type typeOfObjectContainingTheMember,
		string memberName,
		object? valueFoundInvalid,
		string? expectationMakingValueValid = null,
		Exception? innerException = null)
	{
		var messageBuilder = new StringBuilder();
		messageBuilder.Append($"<{typeOfObjectContainingTheMember.Name}> ");
		messageBuilder.Append($"\"{memberName}\" cannot accept value {JsonSerializer.Serialize(valueFoundInvalid ?? "<null>")}");
		if (expectationMakingValueValid is not null)
		{
			messageBuilder.Append($" - {expectationMakingValueValid}");
		}
		if (!string.IsNullOrEmpty(typeOfObjectContainingTheMember.Namespace))
		{
			messageBuilder.Append($" (namespace {typeOfObjectContainingTheMember.Namespace})");
		}

		return new(typeOfObjectContainingTheMember, messageBuilder.ToString(), innerException);
	}

	public static ObjectConstructionException FromInvalidValuesCombination(
		Type typeOfObjectContainingTheMember,
		string memberName1,
		object? valueFoundInvalid1,
		string memberName2,
		object? valueFoundInvalid2,
		string? expectationMakingValueValid = null,
		Exception? innerException = null)
	{
		var messageBuilder = new StringBuilder();
		messageBuilder.Append($"<{typeOfObjectContainingTheMember.Name}> ");
		messageBuilder.Append($"{{\"{memberName1}\", \"{memberName1}\"}} cannot accept value {{{JsonSerializer.Serialize(valueFoundInvalid1 ?? "<null>")},{JsonSerializer.Serialize(valueFoundInvalid2 ?? "<null>")}}}");
		if (expectationMakingValueValid is not null)
		{
			messageBuilder.Append($" - {expectationMakingValueValid}");
		}
		if (!string.IsNullOrEmpty(typeOfObjectContainingTheMember.Namespace))
		{
			messageBuilder.Append($" (namespace {typeOfObjectContainingTheMember.Namespace})");
		}

		return new(typeOfObjectContainingTheMember, messageBuilder.ToString(), innerException);
	}

	public static ObjectConstructionException FromDeveloperMistake(
		Type typeOfObjectUnderConstruction,
		Exception developerMistake)
	=> new(
		typeOfObjectUnderConstruction,
		$"<{typeOfObjectUnderConstruction.Name}> construction failed: {developerMistake.Message} (namespace {typeOfObjectUnderConstruction.Namespace})",
		developerMistake);

	public void AddDebuggingInformation(string informationName, object? informationValue)
	{
		var keyPrefix = $"{_typeOfObjectUnderConstruction.FullName}::{informationName}";
		var numberOfExistingKeysFound = 0;
		while (Data.Contains(BuildDebuggingInformationKey(keyPrefix, numberOfExistingKeysFound)))
		{
			numberOfExistingKeysFound++;
		}
		var uniqueKey = BuildDebuggingInformationKey(keyPrefix, numberOfExistingKeysFound);

		Data.Add(uniqueKey, informationValue ?? "<null>");

		string BuildDebuggingInformationKey(string keyPrefix, int numberOfExistingKeysFound)
		=> numberOfExistingKeysFound switch
		{
			0 => keyPrefix,
			_ => $"{keyPrefix}_{numberOfExistingKeysFound}"
		};
	}
}
