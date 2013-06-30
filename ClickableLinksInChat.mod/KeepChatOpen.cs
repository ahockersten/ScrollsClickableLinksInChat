using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

using ScrollsModLoader.Interfaces;
using UnityEngine;
using Mono.Cecil;

// FIXME check existence of methods, private variables etc and fail gracefully!

namespace ClickableLinksInChat.mod {
    public class ClickableLinksInChat : BaseMod {
        private bool debug = false;

        public ClickableLinksInChat() {
        }

        public static string GetName() {
            return "ClickableLinksInChat";
        }

        public static int GetVersion() {
            return 1;
        }

        public static MethodDefinition[] GetHooks(TypeDefinitionCollection scrollsTypes, int version) {
            return new MethodDefinition[] { };
        }


        public override bool BeforeInvoke(InvocationInfo info, out object returnValue) {
            returnValue = null;
            return false;
        }

        public override void AfterInvoke(InvocationInfo info, ref object returnValue) {
            return;
        }
    }
}
