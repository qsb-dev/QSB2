using System;

namespace QSB2.Utility;

/// <summary>
/// denotes that the given type is used in the unity project
/// and therefore caution should be used when moving/renaming/deleting
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class UsedInUnityProjectAttribute : Attribute;