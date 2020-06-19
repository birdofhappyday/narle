using UnityEngine;
using Assets.Scripts.Utils;
using Assets.Scripts.User;
using System.Collections.Generic;
using System;
using System.Collections;
using Assets.Scripts.Networks.Web;

namespace Assets.Scripts.UI.Windows.Lobby
{
    public class Contact_HideAdmin : UIFriendWndBase
    {
        //  유저 리스트 관련 변수
        public UserViewInfo friendInfo;
        //public UserViewInfoEX friendInfo;

        // 아이디 검색 관련 변수들
        public UIInput uiSearchinput;
        private AutoComplete auto = new AutoComplete();
        private int[] originIndexs;

        public GameObject objOption;

        private bool searchMode = false;
        private string searchWord;

        // Use this for initialization
        public override void Open(params object[] args)
        {
            base.Open(args);
            StartCoroutine(_wait(() =>
           {
               searchMode = false;
                //처음 오픈시 무조건 값 지우기
                uiSearchinput.value = "";

                // 롱터치 초기화
                friendInfo.InitLongTouch("", this);

                // 친구목록 초기화
                friendInfo.InitUserList<st_UserBarData>(0, eUserListType.HideList, eUserType.Hide, eUserRelation.Any);
                //friendInfo.RefreshFriendView();

                // 아이디 검색 초기화
                auto.Init(friendInfo.friendList);
               originIndexs = friendInfo.friendList.userIndexs;

               UIUtil.CloseWnd<LobbyMainWnd>();

                // 통계툴 (숨김친구 화면진입 후)
                PanalyzerUtil.PAN_hideFriendsCnt(friendInfo.uiDataList.Count);
           }));
        }

        private IEnumerator _wait(Action callback)
        {
            yield return null;
            callback();
        }

        public override void RefreshWnd(params object[] args)
        {
            base.RefreshWnd(args);

            if (!searchMode)
            {
                StartCoroutine(_wait(() =>
                {
                   uiSearchinput.value = "";

                    // 롱터치 초기화
                    friendInfo.InitLongTouch("", this);

                    // 친구목록 초기화
                    friendInfo.InitUserList<st_UserBarData>(0, eUserListType.HideList, eUserType.Hide, eUserRelation.Any);
                    //friendInfo.RefreshFriendView();

                    // 아이디 검색 초기화
                    auto.Init(friendInfo.friendList);
                   originIndexs = friendInfo.friendList.userIndexs;

                   UIUtil.CloseWnd<LobbyMainWnd>();

                }));
            }
            else
            {
                StartCoroutine(_wait(() =>
                {
                    // 친구목록 초기화
                    friendInfo.InitUserList<st_UserBarData>(0, eUserListType.HideList, eUserType.Hide, eUserRelation.Any);
                    //friendInfo.RefreshFriendView();

                    // 아이디 검색 초기화
                    auto.Init(friendInfo.friendList);
                    originIndexs = friendInfo.friendList.userIndexs;

                    OnChangeInput(searchWord);

                    UIUtil.CloseWnd<LobbyMainWnd>();
                }));
            }
        }

        public override void RecoverWnd()
        {
            base.RecoverWnd();

            StartCoroutine(_wait(() =>
            {
                searchMode = false;
                //처음 오픈시 무조건 값 지우기
                uiSearchinput.value = "";

                // 롱터치 초기화
                friendInfo.InitLongTouch("", this);

                // 친구목록 초기화
                friendInfo.InitUserList<st_UserBarData>(0, eUserListType.HideList, eUserType.Hide, eUserRelation.Any);
                //friendInfo.RefreshFriendView();

                // 아이디 검색 초기화
                auto.Init(friendInfo.friendList);
                originIndexs = friendInfo.friendList.userIndexs;

                UIUtil.CloseWnd<LobbyMainWnd>();
            }));
        }

        public void OnChangeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                searchMode = false;
                friendInfo.RefreshFriendList<st_UserBarData>(0, originIndexs);
            }
            else
            {
                searchMode = true;
                searchWord = input;

                auto.MakeSearchList(input);
                List<int> list = new List<int>();

                try
                {
                    for (int i = 0, max = auto.resultSearch.Count; i < max; i++)
                        list.Add(auto.resultSearch[i].index);//originIndexs[auto.resultSearch[i].index]);

                    friendInfo.RefreshFriendList<st_UserBarData>(0, list.ToArray());
                }

                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        public override void OnClick_Back()
        {
            if (UIUtil.IsSwiping())
                return;

            UIUtil.Swipe(eWindow.Contact_FriendSetting);
        }
    }
}
