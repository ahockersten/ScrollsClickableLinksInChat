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
        private Dictionary<ChatRooms.RoomLog, Dictionary<ChatRooms.ChatLine, string>> roomLinkCache = new Dictionary<ChatRooms.RoomLog, Dictionary<ChatRooms.ChatLine, string>>();

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
            return 3;
        }

        public static MethodDefinition[] GetHooks(TypeDefinitionCollection scrollsTypes, int version) {
            try {
                return new MethodDefinition[] {
                    scrollsTypes["ChatUI"].Methods.GetMethod("OnGUI")[0],
                    scrollsTypes["ChatRooms"].Methods.GetMethod("LeaveRoom", new Type[]{typeof(string)})
                };
            }
            catch {
                return new MethodDefinition[] { };
            }
        }

        public override bool BeforeInvoke(InvocationInfo info, out object returnValue) {
            if (info.target is ChatRooms && info.targetMethod.Equals("LeaveRoom")) {
                string room = (string) info.arguments[0];
                Dictionary<string, ChatRooms.RoomLog> chatLogs = (Dictionary<string, ChatRooms.RoomLog>)typeof(ChatRooms).GetField("chatLogs", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(info.target);
                if (chatLogs.ContainsKey(room)) {
                    if (roomLinkCache.ContainsKey(chatLogs[room])) {
                        roomLinkCache.Remove(chatLogs[room]);
                    }
                }
            }
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

                    // set invisible draw color. We want the layout effects of drawing stuff, but we let the 
                    // original code do all of the actual drawing
                    Color oldColor = GUI.color;
                    GUI.color = debug ? Color.cyan : Color.clear;
                    GUILayout.BeginArea(chatlogAreaInner);
                    GUILayout.BeginScrollView(chatScroll, new GUILayoutOption[] { GUILayout.Width(chatlogAreaInner.width), GUILayout.Height(chatlogAreaInner.height) });
                    foreach (ChatRooms.ChatLine current in currentRoomChatLog.getLines()) {
                        GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                        GUILayout.Label(current.timestamp, timeStampStyle, new GUILayoutOption[] {
                            GUILayout.Width(20f + (float)Screen.height * 0.042f)});

                        if (!roomLinkCache.ContainsKey(currentRoomChatLog)) {
                            roomLinkCache.Add(currentRoomChatLog, new Dictionary<ChatRooms.ChatLine, string>());
                        }
                        Dictionary<ChatRooms.ChatLine, string> linkCache = roomLinkCache[currentRoomChatLog];
                        if (!linkCache.ContainsKey(current)) {
                            Match linkFound = linkFinder.Match(current.text);
                            if (linkFound.Success) {
                                linkCache.Add(current, linkFound.Value);
                            }
                            else {
                                linkCache.Add(current, null);
                            }
                        }
                        
                        if (linkCache[current] != null) {
                            if (GUILayout.Button(current.text, chatLogStyle, new GUILayoutOption[] { GUILayout.Width(chatlogAreaInner.width - (float)Screen.height * 0.1f - 20f) })) {
                                Process.Start((string)linkCache[current]);
                            }
                        }
                        else {
                            GUILayout.Label(current.text, chatLogStyle, new GUILayoutOption[] {
				                GUILayout.Width(chatlogAreaInner.width - (float)Screen.height * 0.1f - 20f)});
                        }
                        GUILayout.EndHorizontal();
                    }
                    GUILayout.EndScrollView();
                    GUILayout.EndArea();
                    // restore old color. Should not be necessary, but it does not hurt to be paranoid
                    GUI.color = oldColor;
                }
            }
        }
    }
}
