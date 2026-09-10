using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Icod.DCurses.Tests;

/// <summary>Guards the complete current public Icod.DCurses assembly contract.</summary>
public sealed class PublicApiFingerprintTests {
	[Fact]
	public void PublicApiMatchesCurrentDevelopmentFingerprint() {
		PublicApiFingerprint actual = PublicApiFingerprint.Create(
			typeof( CursesSession ).Assembly
		);
		string baselinePath = Path.Combine(
			AppContext.BaseDirectory,
			"Public-API-Fingerprint-1.2.json"
		);
		using JsonDocument document = JsonDocument.Parse(
			File.ReadAllText( baselinePath )
		);
		JsonElement root = document.RootElement;
		string expectedSha256 = root.GetProperty( "sha256" ).GetString()!;
		int expectedExportedTypeCount = root.GetProperty( "exportedTypeCount" ).GetInt32();
		int expectedContractLineCount = root.GetProperty( "contractLineCount" ).GetInt32();
		string[] expectedTypes = root.GetProperty( "exportedTypes" )
			.EnumerateArray()
			.Select( static current => current.GetString()! )
			.ToArray();

		Assert.True(
			string.Equals(
				expectedSha256,
				actual.Sha256,
				StringComparison.Ordinal
			),
			CreateMismatchMessage( actual )
		);
		Assert.Equal( expectedExportedTypeCount, actual.ExportedTypes.Length );
		Assert.Equal( expectedContractLineCount, actual.ContractLines.Length );
		Assert.Equal( expectedTypes, actual.ExportedTypes );
	}

	[Fact]
	public void StableOneOneExportedTypesRemainPresent() {
		PublicApiFingerprint actual = PublicApiFingerprint.Create(
			typeof( CursesSession ).Assembly
		);
		string baselinePath = Path.Combine(
			AppContext.BaseDirectory,
			"Public-API-Fingerprint-1.1.json"
		);
		using JsonDocument document = JsonDocument.Parse(
			File.ReadAllText( baselinePath )
		);
		string[] stableTypes = document.RootElement
			.GetProperty( "exportedTypes" )
			.EnumerateArray()
			.Select( static current => current.GetString()! )
			.ToArray();

		foreach ( string stableType in stableTypes ) {
			Assert.Contains(
				stableType,
				actual.ExportedTypes
			);
		}
	}

	private static string CreateMismatchMessage( PublicApiFingerprint actual ) {
		ArgumentNullException.ThrowIfNull( actual );
		StringBuilder builder = new();
		_ = builder.AppendLine( "Public API fingerprint mismatch." );
		_ = builder.Append( "sha256=" ).AppendLine( actual.Sha256 );
		_ = builder.Append( "exportedTypeCount=" )
			.AppendLine( actual.ExportedTypes.Length.ToString( CultureInfo.InvariantCulture ) );
		_ = builder.Append( "contractLineCount=" )
			.AppendLine( actual.ContractLines.Length.ToString( CultureInfo.InvariantCulture ) );
		_ = builder.AppendLine( "exportedTypes:" );
		foreach ( string type in actual.ExportedTypes ) {
			_ = builder.Append( "  " ).AppendLine( type );
		}
		return builder.ToString();
	}

	private sealed class PublicApiFingerprint {
		private PublicApiFingerprint(
			string sha256,
			string[] exportedTypes,
			string[] contractLines
		) {
			this.Sha256 = sha256;
			this.ExportedTypes = exportedTypes;
			this.ContractLines = contractLines;
		}

		internal string Sha256 { get; }

		internal string[] ExportedTypes { get; }

		internal string[] ContractLines { get; }

		internal static PublicApiFingerprint Create( Assembly assembly ) {
			ArgumentNullException.ThrowIfNull( assembly );
			Type[] exportedTypes = assembly.GetExportedTypes()
				.OrderBy( static type => FormatType( type ), StringComparer.Ordinal )
				.ToArray();
			List<string> lines = [];
			NullabilityInfoContext nullability = new();

			foreach ( Type type in exportedTypes ) {
				AddType( type, lines );
				if ( type.IsEnum ) {
					AddEnum( type, lines );
					continue;
				}

				foreach ( ConstructorInfo constructor in type.GetConstructors(
					BindingFlags.Public
						| BindingFlags.Instance
						| BindingFlags.DeclaredOnly
				) ) {
					lines.Add(
						$"CTOR|{FormatType( type )}|{FormatParameters( constructor.GetParameters(), nullability )}"
					);
				}

				foreach ( FieldInfo field in type.GetFields(
					BindingFlags.Public
						| BindingFlags.Instance
						| BindingFlags.Static
						| BindingFlags.DeclaredOnly
				) ) {
					lines.Add( FormatField( type, field, nullability ) );
				}

				foreach ( PropertyInfo property in type.GetProperties(
					BindingFlags.Public
						| BindingFlags.Instance
						| BindingFlags.Static
						| BindingFlags.DeclaredOnly
				) ) {
					lines.Add( FormatProperty( type, property, nullability ) );
				}

				foreach ( EventInfo eventInfo in type.GetEvents(
					BindingFlags.Public
						| BindingFlags.Instance
						| BindingFlags.Static
						| BindingFlags.DeclaredOnly
				) ) {
					lines.Add( FormatEvent( type, eventInfo, nullability ) );
				}

				foreach ( MethodInfo method in type.GetMethods(
					BindingFlags.Public
						| BindingFlags.Instance
						| BindingFlags.Static
						| BindingFlags.DeclaredOnly
				) ) {
					if ( IsAccessor( method ) ) {
						continue;
					}
					lines.Add( FormatMethod( type, method, nullability ) );
				}
			}

			string[] contractLines = lines
				.OrderBy( static line => line, StringComparer.Ordinal )
				.ToArray();
			string canonical = string.Join( "\n", contractLines ) + "\n";
			string sha256 = Convert.ToHexString(
				SHA256.HashData( Encoding.UTF8.GetBytes( canonical ) )
			).ToLowerInvariant();
			return new PublicApiFingerprint(
				sha256,
				exportedTypes.Select( static type => FormatType( type ) ).ToArray(),
				contractLines
			);
		}

		private static void AddType(
			Type type,
			List<string> lines
		) {
			ArgumentNullException.ThrowIfNull( type );
			ArgumentNullException.ThrowIfNull( lines );
			string baseType = type.BaseType is null
				? "-"
				: FormatType( type.BaseType )
			;
			string interfaces = string.Join(
				",",
				type.GetInterfaces()
					.Select( static current => FormatType( current ) )
					.OrderBy( static current => current, StringComparer.Ordinal )
			);
			string generic = FormatGenericParameters( type.GetGenericArguments() );
			lines.Add(
				$"TYPE|{GetTypeKind( type )}|{FormatType( type )}|base={baseType}|interfaces={interfaces}|generic={generic}|sealed={type.IsSealed}|abstract={type.IsAbstract}"
			);
		}

		private static void AddEnum(
			Type type,
			List<string> lines
		) {
			ArgumentNullException.ThrowIfNull( type );
			ArgumentNullException.ThrowIfNull( lines );
			Type underlying = Enum.GetUnderlyingType( type );
			string values = string.Join(
				",",
				Enum.GetNames( type ).Select(
					name => $"{name}={FormatEnumValue( Enum.Parse( type, name ), underlying )}"
				)
			);
			lines.Add(
				$"ENUM|{FormatType( type )}|underlying={FormatType( underlying )}|flags={type.IsDefined( typeof( FlagsAttribute ), false )}|values={values}"
			);
		}

		private static string FormatField(
			Type declaringType,
			FieldInfo field,
			NullabilityInfoContext nullability
		) {
			ArgumentNullException.ThrowIfNull( declaringType );
			ArgumentNullException.ThrowIfNull( field );
			ArgumentNullException.ThrowIfNull( nullability );
			string literal = field.IsLiteral
				? FormatDefaultValue( field.GetRawConstantValue() )
				: "-"
			;
			return $"FIELD|{FormatType( declaringType )}|{( field.IsStatic ? "static" : "instance" )}|{FormatType( field.FieldType )}|{field.Name}|readonly={field.IsInitOnly}|literal={literal}|null={FormatNullability( nullability.Create( field ) )}";
		}

		private static string FormatProperty(
			Type declaringType,
			PropertyInfo property,
			NullabilityInfoContext nullability
		) {
			ArgumentNullException.ThrowIfNull( declaringType );
			ArgumentNullException.ThrowIfNull( property );
			ArgumentNullException.ThrowIfNull( nullability );
			MethodInfo? getMethod = property.GetMethod;
			MethodInfo? setMethod = property.SetMethod;
			bool isStatic = ( getMethod ?? setMethod )?.IsStatic ?? false;
			return $"PROPERTY|{FormatType( declaringType )}|{( isStatic ? "static" : "instance" )}|{FormatType( property.PropertyType )}|{property.Name}|index={FormatParameters( property.GetIndexParameters(), nullability )}|get={FormatAccessor( getMethod )}|set={FormatAccessor( setMethod )}|null={FormatNullability( nullability.Create( property ) )}";
		}

		private static string FormatEvent(
			Type declaringType,
			EventInfo eventInfo,
			NullabilityInfoContext nullability
		) {
			ArgumentNullException.ThrowIfNull( declaringType );
			ArgumentNullException.ThrowIfNull( eventInfo );
			ArgumentNullException.ThrowIfNull( nullability );
			MethodInfo? addMethod = eventInfo.AddMethod;
			return $"EVENT|{FormatType( declaringType )}|{( addMethod?.IsStatic == true ? "static" : "instance" )}|{FormatType( eventInfo.EventHandlerType! )}|{eventInfo.Name}|add={FormatAccessor( addMethod )}|remove={FormatAccessor( eventInfo.RemoveMethod )}|null={FormatNullability( nullability.Create( eventInfo ) )}";
		}

		private static string FormatMethod(
			Type declaringType,
			MethodInfo method,
			NullabilityInfoContext nullability
		) {
			ArgumentNullException.ThrowIfNull( declaringType );
			ArgumentNullException.ThrowIfNull( method );
			ArgumentNullException.ThrowIfNull( nullability );
			string generic = FormatGenericParameters( method.GetGenericArguments() );
			return $"METHOD|{FormatType( declaringType )}|{( method.IsStatic ? "static" : "instance" )}|{FormatType( method.ReturnType )}|{method.Name}|generic={generic}|params={FormatParameters( method.GetParameters(), nullability )}|returnNull={FormatNullability( nullability.Create( method.ReturnParameter ) )}";
		}

		private static string FormatParameters(
			IEnumerable<ParameterInfo> parameters,
			NullabilityInfoContext nullability
		) {
			ArgumentNullException.ThrowIfNull( parameters );
			ArgumentNullException.ThrowIfNull( nullability );
			return string.Join(
				",",
				parameters.Select(
					parameter => $"{FormatRefKind( parameter )}:{FormatType( parameter.ParameterType )}:{parameter.Name}:optional={parameter.IsOptional}:default={FormatDefaultValue( parameter.HasDefaultValue ? parameter.DefaultValue : Missing.Value )}:null={FormatNullability( nullability.Create( parameter ) )}"
				)
			);
		}

		private static string FormatGenericParameters( IEnumerable<Type> arguments ) {
			ArgumentNullException.ThrowIfNull( arguments );
			return string.Join(
				";",
				arguments
					.Where( static argument => argument.IsGenericParameter )
					.Select(
						static argument => $"{argument.Name}:{argument.GenericParameterAttributes}:{string.Join( ",", argument.GetGenericParameterConstraints().Select( FormatType ).OrderBy( static value => value, StringComparer.Ordinal ) )}"
					)
			);
		}

		private static string FormatType( Type type ) {
			ArgumentNullException.ThrowIfNull( type );
			if ( type.IsByRef ) {
				return $"{FormatType( type.GetElementType()! )}&";
			}
			if ( type.IsPointer ) {
				return $"{FormatType( type.GetElementType()! )}*";
			}
			if ( type.IsArray ) {
				return $"{FormatType( type.GetElementType()! )}[{new string( ',', type.GetArrayRank() - 1 )}]";
			}
			if ( type.IsGenericParameter ) {
				return $"!{type.Name}";
			}
			if ( type.IsGenericType ) {
				Type definition = type.GetGenericTypeDefinition();
				return $"{definition.FullName}[{string.Join( ",", type.GetGenericArguments().Select( FormatType ) )}]";
			}
			return type.FullName ?? type.Name;
		}

		private static string FormatNullability( NullabilityInfo info ) {
			ArgumentNullException.ThrowIfNull( info );
			string arguments = info.GenericTypeArguments.Length == 0
				? string.Empty
				: $"<{string.Join( ",", info.GenericTypeArguments.Select( FormatNullability ) )}>"
			;
			string element = info.ElementType is null
				? string.Empty
				: $"[{FormatNullability( info.ElementType )}]"
			;
			return $"{info.ReadState}/{info.WriteState}{arguments}{element}";
		}

		private static string FormatRefKind( ParameterInfo parameter ) {
			ArgumentNullException.ThrowIfNull( parameter );
			if ( !parameter.ParameterType.IsByRef ) {
				return "value";
			}
			if ( parameter.IsOut ) {
				return "out";
			}
			if ( parameter.IsIn ) {
				return "in";
			}
			return "ref";
		}

		private static string FormatDefaultValue( object? value ) {
			return value switch {
				null => "null",
				Missing => "-",
				DBNull => "dbnull",
				string text => $"\"{text.Replace( "\\", "\\\\", StringComparison.Ordinal ).Replace( "\"", "\\\"", StringComparison.Ordinal )}\"",
				char character => $"'{character}'",
				bool boolean => boolean ? "true" : "false",
				IFormattable formattable => formattable.ToString( null, CultureInfo.InvariantCulture ) ?? string.Empty,
				_ => value.ToString() ?? string.Empty
			};
		}

		private static string FormatEnumValue(
			object value,
			Type underlying
		) {
			ArgumentNullException.ThrowIfNull( value );
			ArgumentNullException.ThrowIfNull( underlying );
			object converted = Convert.ChangeType(
				value,
				underlying,
				CultureInfo.InvariantCulture
			);
			return Convert.ToString( converted, CultureInfo.InvariantCulture )!;
		}

		private static string FormatAccessor( MethodInfo? method ) {
			if ( method is null ) {
				return "none";
			}
			return method.IsPublic ? "public" : "nonpublic";
		}

		private static bool IsAccessor( MethodInfo method ) {
			ArgumentNullException.ThrowIfNull( method );
			return method.IsSpecialName
				&& (
					method.Name.StartsWith( "get_", StringComparison.Ordinal )
						|| method.Name.StartsWith( "set_", StringComparison.Ordinal )
						|| method.Name.StartsWith( "add_", StringComparison.Ordinal )
						|| method.Name.StartsWith( "remove_", StringComparison.Ordinal )
				);
		}

		private static string GetTypeKind( Type type ) {
			ArgumentNullException.ThrowIfNull( type );
			if ( type.IsEnum ) {
				return "enum";
			}
			if ( type.IsInterface ) {
				return "interface";
			}
			if ( type.IsValueType ) {
				return "struct";
			}
			if ( typeof( MulticastDelegate ).IsAssignableFrom( type.BaseType ) ) {
				return "delegate";
			}
			return "class";
		}
	}
}
