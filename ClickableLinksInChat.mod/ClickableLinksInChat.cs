using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

using ScrollsModLoader.Interfaces;
using UnityEngine;
using Mono.Cecil;

namespace ClickableLinksInChat.mod {
    public class ClickableLinksInChat : BaseMod {
        private bool debug = false;
        private ChatUI target = null;
        private ChatRooms chatRooms;
        private GUIStyle timeStampStyle;
        private GUIStyle chatLogStyle;
        private Regex linkFinder;

        public ClickableLinksInChat() {
            // from http://daringfireball.net/2010/07/improved_regex_for_matching_urls
            // I had to remove a " in there to make it work, but it should match well enough anyway
            linkFinder = new Regex(@"(?i)\b((?:[a-z][\w-]+:(?:/{1,3}|[a-z0-9%])|www\d{0,3}[.]|[a-z0-9.\-]+[.][a-z]{2,4}/)(?:[^\s()<>]+|\(([^\s()<>]+|(\([^\s()<>]+\)))*\))+(?:\(([^\s()<>]+|(\([^\s()<>]+\)))*\)|[^\s`!()\[\]{};:'.,<>?«»“”‘’]))"
                /*, RegexOptions.Compiled*/); // compiled regexes are not supported in the version of Unity used by Scrolls :(
        }

        public static string GetName() {
            return "ClickableLinksInChat";
        }

        public static int GetVersion() {
            return 1;
        }

        public static MethodDefinition[] GetHooks(TypeDefinitionCollection scrollsTypes, int version) {
            try {
                return new MethodDefinition[] {
                    scrollsTypes["ChatUI"].Methods.GetMethod("OnGUI")[0]
                };
            }
            catch {
                Console.WriteLine("ClickableLinksInChat failed to connect to methods used.");
                return new MethodDefinition[] { };
            }
        }

        public override bool BeforeInvoke(InvocationInfo info, out object returnValue) {
            returnValue = null;
            return false;
        }

        public override void AfterInvoke(InvocationInfo info, ref object returnValue) {
            if (info.target is ChatUI && info.targetMethod.Equals("OnGUI")) {
                if (target != (ChatUI)info.target) {
                    chatRooms = (ChatRooms)typeof(ChatUI).GetField("chatRooms", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(info.target);
                    timeStampStyle = (GUIStyle)typeof(ChatUI).GetField("timeStampStyle", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(info.target);
                    chatLogStyle = (GUIStyle)typeof(ChatUI).GetField("chatLogStyle", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(info.target);
                    target = (ChatUI)info.target;
                }
                ChatRooms.RoomLog currentRoomChatLog = chatRooms.GetCurrentRoomChatLog();
                if (currentRoomChatLog != null) {
                    // these need to be refetched on every run, because otherwise old values will be used
                    Rect chatlogAreaInner = (Rect)typeof(ChatUI).GetField("chatlogAreaInner", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(info.target);
                    Vector2 chatScroll = (Vector2)typeof(ChatUI).GetField("chatScroll", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(info.target);

                    GUILayout.BeginArea(chatlogAreaInner);
                    GUILayout.BeginScrollView(chatScroll, new GUILayoutOption[] { GUILayout.Width(chatlogAreaInner.width), GUILayout.Height(chatlogAreaInner.height) });
                    foreach (ChatRooms.ChatLine current in currentRoomChatLog.getLines()) {
                        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                        // set invisible draw color. We want the layout effects of drawing stuff, but we let the 
                        // original code do all of the actual drawing
                        Color oldColor = GUI.color;
                        GUI.color = debug ? Color.cyan : Color.clear;
                        GUILayout.Label(current.timestamp, timeStampStyle, new GUILayoutOption[] {
                            GUILayout.Width(20f + (float)Screen.height * 0.042f)});
                        Match linkFound = linkFinder.Match(current.text);
                        if (linkFound.Success) {
                            if (GUILayout.Button(current.text, chatLogStyle, new GUILayoutOption[] { GUILayout.Width(chatlogAreaInner.width - (float)Screen.height * 0.1f - 20f) })) {
                                Process.Start(linkFound.Value);
                            }
                        }
                        else {
                            GUILayout.Label(current.text, chatLogStyle, new GUILayoutOption[] {
				                GUILayout.Width(chatlogAreaInner.width - (float)Screen.height * 0.1f - 20f)});
                        }

                        // restore old color. Should not be necessary, but it does not hurt to be paranoid
                        GUI.color = oldColor;
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();
                    GUILayout.EndArea();
                }
            }
        }
    }
}
