// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET

using System.Collections.Generic;

/// <summary>
/// Benchmarks for real-world MessagePack patterns not covered by existing benchmarks:
/// nested objects, dictionaries, string-heavy data, enum arrays, and SignalR-like messages.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
public partial class RealWorldPatterns
{
	private static readonly MessagePackSerializer Serializer = new() { SerializeDefaultValues = SerializeDefaultValuesPolicy.Always };

	// --- Nested object (SignalR-like hub message) ---
	private static readonly HubMessage HubMessageValue = new()
	{
		MethodName = "SendMessage",
		InvocationId = "inv-42",
		Sender = new UserInfo { UserId = 1001, DisplayName = "Alice", Email = "alice@example.com" },
		Timestamp = new DateTime(2026, 4, 7, 12, 0, 0, DateTimeKind.Utc),
		Tags = ["general", "chat", "public"],
	};

	private static readonly byte[] HubMessageMsgPack = Serializer.Serialize<HubMessage, Witness>(HubMessageValue);

	// --- Dictionary ---
	private static readonly Dictionary<string, int> DictionaryValue = CreateDictionary(100);
	private static readonly byte[] DictionaryMsgPack = Serializer.Serialize<Dictionary<string, int>, Witness>(DictionaryValue);

	// --- String array ---
	private static readonly string[] StringArrayValue = CreateStringArray(1000);
	private static readonly byte[] StringArrayMsgPack = Serializer.Serialize<string[], Witness>(StringArrayValue);

	// --- Enum array ---
	private static readonly StatusCode[] EnumArrayValue = CreateEnumArray(10_000);
	private static readonly byte[] EnumArrayMsgPack = Serializer.Serialize<StatusCode[], Witness>(EnumArrayValue);

	// --- Array of nested POCOs ---
	private static readonly UserInfo[] UserArrayValue = CreateUserArray(100);
	private static readonly byte[] UserArrayMsgPack = Serializer.Serialize<UserInfo[], Witness>(UserArrayValue);

	private readonly Sequence buffer = new();

	// ===== Nested Object (SignalR-like) =====

	[Benchmark]
	[BenchmarkCategory("nested", "serialize")]
	public void NestedObject_Serialize()
	{
		Serializer.Serialize<HubMessage, Witness>(this.buffer, HubMessageValue);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("nested", "deserialize")]
	public HubMessage? NestedObject_Deserialize()
	{
		return Serializer.Deserialize<HubMessage, Witness>(HubMessageMsgPack);
	}

	// ===== Dictionary =====

	[Benchmark]
	[BenchmarkCategory("dictionary", "serialize")]
	public void Dictionary_Serialize()
	{
		Serializer.Serialize<Dictionary<string, int>, Witness>(this.buffer, DictionaryValue);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("dictionary", "deserialize")]
	public Dictionary<string, int>? Dictionary_Deserialize()
	{
		return Serializer.Deserialize<Dictionary<string, int>, Witness>(DictionaryMsgPack);
	}

	// ===== String Array =====

	[Benchmark]
	[BenchmarkCategory("strings", "serialize")]
	public void StringArray_Serialize()
	{
		Serializer.Serialize<string[], Witness>(this.buffer, StringArrayValue);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("strings", "deserialize")]
	public string[]? StringArray_Deserialize()
	{
		return Serializer.Deserialize<string[], Witness>(StringArrayMsgPack);
	}

	// ===== Enum Array =====

	[Benchmark]
	[BenchmarkCategory("enums", "serialize")]
	public void EnumArray_Serialize()
	{
		Serializer.Serialize<StatusCode[], Witness>(this.buffer, EnumArrayValue);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("enums", "deserialize")]
	public StatusCode[]? EnumArray_Deserialize()
	{
		return Serializer.Deserialize<StatusCode[], Witness>(EnumArrayMsgPack);
	}

	// ===== Array of POCOs =====

	[Benchmark]
	[BenchmarkCategory("poco-array", "serialize")]
	public void UserArray_Serialize()
	{
		Serializer.Serialize<UserInfo[], Witness>(this.buffer, UserArrayValue);
		this.buffer.Reset();
	}

	[Benchmark]
	[BenchmarkCategory("poco-array", "deserialize")]
	public UserInfo[]? UserArray_Deserialize()
	{
		return Serializer.Deserialize<UserInfo[], Witness>(UserArrayMsgPack);
	}

	// ===== Data setup =====

	private static Dictionary<string, int> CreateDictionary(int count)
	{
		var dict = new Dictionary<string, int>(count);
		for (int i = 0; i < count; i++)
		{
			dict[$"key_{i:D4}"] = i * 7;
		}

		return dict;
	}

	private static string[] CreateStringArray(int count)
	{
		Random random = new(123);
		string[] values = new string[count];
		for (int i = 0; i < count; i++)
		{
			values[i] = $"item_{random.Next(0, 10000):D5}_{(char)('a' + (i % 26))}";
		}

		return values;
	}

	private static StatusCode[] CreateEnumArray(int count)
	{
		Random random = new(123);
		var codes = Enum.GetValues<StatusCode>();
		StatusCode[] values = new StatusCode[count];
		for (int i = 0; i < count; i++)
		{
			values[i] = codes[random.Next(codes.Length)];
		}

		return values;
	}

	private static UserInfo[] CreateUserArray(int count)
	{
		UserInfo[] values = new UserInfo[count];
		for (int i = 0; i < count; i++)
		{
			values[i] = new UserInfo
			{
				UserId = i + 1,
				DisplayName = $"User {i + 1}",
				Email = $"user{i + 1}@example.com",
			};
		}

		return values;
	}

	[GenerateShapeFor<HubMessage>]
	[GenerateShapeFor<UserInfo>]
	[GenerateShapeFor<Dictionary<string, int>>]
	[GenerateShapeFor<string[]>]
	[GenerateShapeFor<StatusCode[]>]
	[GenerateShapeFor<UserInfo[]>]
	private partial class Witness;
}

/// <summary>A SignalR-like hub invocation message with nested objects.</summary>
[GenerateShape]
public partial class HubMessage
{
	public string? MethodName { get; set; }

	public string? InvocationId { get; set; }

	public UserInfo? Sender { get; set; }

	public DateTime Timestamp { get; set; }

	public List<string>? Tags { get; set; }
}

/// <summary>A user info object used in nested and array benchmarks.</summary>
[GenerateShape]
public partial class UserInfo
{
	public int UserId { get; set; }

	public string? DisplayName { get; set; }

	public string? Email { get; set; }
}

/// <summary>Small enum for real-world enum array benchmarks (values fit in fixint).</summary>
public enum StatusCode
{
	Unknown = 0,
	Active = 1,
	Inactive = 2,
	Pending = 3,
	Approved = 4,
	Rejected = 5,
	Deleted = 6,
	Archived = 7,
}

#endif
