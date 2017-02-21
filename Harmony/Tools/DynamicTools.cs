﻿using Harmony.ILCopying;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Harmony
{
	public static class DynamicTools
	{
		public static DynamicMethod CreateDynamicMethod(MethodBase original, string suffix)
		{
			if (original == null) throw new Exception("original cannot be null");
			var patchName = original.Name + suffix;
			patchName = patchName.Replace("<>", "");

			var parameters = original.GetParameters();
			var result = parameters.Types().ToList();
			if (original.IsStatic == false)
				result.Insert(0, typeof(object));
			var paramTypes = result.ToArray();

			var method = new DynamicMethod(
				patchName,
				MethodAttributes.Public | (original.IsStatic ? MethodAttributes.Static : 0),
				CallingConventions.Standard,
				AccessTools.GetReturnedType(original),
				paramTypes,
				original.DeclaringType,
				true
			);

			for (int i = 0; i < parameters.Length; i++)
				method.DefineParameter(i + 1, parameters[i].Attributes, parameters[i].Name);

			return method;
		}

		public static LocalBuilder[] DeclareLocalVariables(MethodBase original, ILGenerator il)
		{
			return original.GetMethodBody().LocalVariables.Select(
				lvi => il.DeclareLocal(lvi.LocalType, lvi.IsPinned)
			).ToArray();
		}

		public static LocalBuilder DeclareReturnVar(MethodBase original, ILGenerator il)
		{
			var type = AccessTools.GetReturnedType(original);
			if (AccessTools.isClass(type))
			{
				var v = il.DeclareLocal(type);
				il.Emit(OpCodes.Ldnull);
				if (MethodCopier.DEBUG_OPCODES) FileLog.Log("# " + OpCodes.Ldnull);
				il.Emit(OpCodes.Stloc, v);
				if (MethodCopier.DEBUG_OPCODES) FileLog.Log("# " + OpCodes.Stloc + " " + v);
				return v;
			}
			if (AccessTools.isStruct(type))
			{
				var v = il.DeclareLocal(type);
				il.Emit(OpCodes.Ldloca, v);
				if (MethodCopier.DEBUG_OPCODES) FileLog.Log("# " + OpCodes.Ldloca + " " + v);
				il.Emit(OpCodes.Initobj, type);
				if (MethodCopier.DEBUG_OPCODES) FileLog.Log("# " + OpCodes.Initobj + " " + type);
				return v;
			}
			if (AccessTools.isValue(type))
			{
				var v = il.DeclareLocal(type);
				if (type == typeof(float))
				{
					il.Emit(OpCodes.Ldc_R4, (float)0);
					if (MethodCopier.DEBUG_OPCODES) FileLog.Log("# " + OpCodes.Ldc_R4 + " " + (float)0);
				}
				else if (type == typeof(double))
				{
					il.Emit(OpCodes.Ldc_R8, (double)0);
					if (MethodCopier.DEBUG_OPCODES) FileLog.Log("# " + OpCodes.Ldc_R8 + " " + (double)0);
				}
				else if (type == typeof(long))
				{
					il.Emit(OpCodes.Ldc_I8, (long)0);
					if (MethodCopier.DEBUG_OPCODES) FileLog.Log("# " + OpCodes.Ldc_I8 + " " + (long)0);
				}
				else
				{
					il.Emit(OpCodes.Ldc_I4, 0);
					if (MethodCopier.DEBUG_OPCODES) FileLog.Log("# " + OpCodes.Ldc_I4 + " " + 0);
				}
				il.Emit(OpCodes.Stloc, v);
				if (MethodCopier.DEBUG_OPCODES) FileLog.Log("# " + OpCodes.Stloc + " " + v);
				return v;
			}
			return null;
		}

		public static void PrepareDynamicMethod(DynamicMethod method)
		{
			var m_CreateDynMethod = typeof(DynamicMethod).GetMethod("CreateDynMethod", BindingFlags.NonPublic | BindingFlags.Instance);
			m_CreateDynMethod.Invoke(method, new object[0]);
		}
	}
}