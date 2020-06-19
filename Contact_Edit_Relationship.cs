using UnityEngine;
using Assets.Scripts.Utils;
using Core.UI;
using Assets.Scripts.User;
using System;
using System.Collections.Generic;
using Assets.Scripts.Cores.LocalDB;
using Assets.Scripts.Networks.Web;
using Assets.Scripts.UI.Windows.Popup;
using System.Linq;
using System.Collections;

namespace Assets.Scripts.UI.Windows.Lobby
{
    public class Contact_Edit_Relationship : UIFriendWndBase
    {
        public UserViewInfoEX friendInfo;
        public GameObject favortyForm;
        public GameObject favortyScroll;
        public GameObject firendNumber;
        public UILabel friendLabel;
        public UIInput SearchInput;

        static public readonly int Friend = 0;
        static public readonly int Favorites = 1;

        private AutoComplete auto = new AutoComplete();
        private int[] originIndexs;

        private bool searchMode = false;
        private string searchWord;

        // Use this for initialization
        public override void Open(params object[] args)
        {
            base.Open();

            UIUtil.OpenStopWnd();

            StartCoroutine(_wait(() =>
            {
                searchMode = false;

                SearchInput.value = "";
                friendLabel.text = "친구";
                firendNumber.SetActive(true);
                //롱터치 초기화
                friendInfo.InitLongTouch("", this);
                //친구 목록 초기화
                friendInfo.InitUserList<st_UserToggleData>(Favorites,
                    eUserListType.FavoryFixList, eUserType.Favori, eUserRelation.Friend, UIUtil.GetWnd<ContactWnd>().SortFavoryFriendList);

                //친구목록 초기화
                friendInfo.InitUserList<st_UserToggleData>(Friend, eUserListType.FavoryFixSimpleList,
                    new eUserType[] { eUserType.Normal, eUserType.Favori }, new eUserRelation[] { eUserRelation.Friend }
                    , UIUtil.GetWnd<ContactWnd>().SortFriendList);

                //아이디 검색 초기화
                auto.Init(friendInfo.friendList[Friend]);
                originIndexs = friendInfo.friendList[Friend].userIndexs;

                if (0 == friendInfo.uiDataList[Favorites].Count)
                {
                    favortyForm.SetActive(false);
                    favortyScroll.SetActive(false);
                }
                else
                {
                    favortyForm.SetActive(true);
                    favortyScroll.SetActive(true);
                }

                friendInfo.RefreshFriendView();
                friendInfo.friendView.ResetPosition();

                UIUtil.CloseWnd<LobbyMainWnd>();
                UIUtil.CloseStopWnd();

				// 통계툴 (C007 : 친구 목록 편집)
				PanalyzerUtil.PAN_menu("C007");
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
                UIUtil.OpenStopWnd();

                StartCoroutine(_wait(() =>
                {
                    friendInfo.RefreshFriendList<st_UserToggleData>(
                        Favorites,
                        eUserListType.FavoryFixList,
                        new eUserType[] { eUserType.Favori },
                        new eUserRelation[] { eUserRelation.Friend }, UIUtil.GetWnd<ContactWnd>().SortFavoryFriendList);

                //친구목록 갱신
                friendInfo.RefreshFriendList<st_UserToggleData>(Friend, eUserListType.FavoryFixSimpleList,
                        new eUserType[] { eUserType.Normal, eUserType.Favori }, new eUserRelation[] { eUserRelation.Friend }
                        , UIUtil.GetWnd<ContactWnd>().SortFriendList);

                    auto.Init(friendInfo.friendList[Friend]);
                    originIndexs = friendInfo.friendList[Friend].userIndexs;

                    if (0 == friendInfo.uiDataList[Favorites].Count)
                    {
                        favortyForm.SetActive(false);
                        friendLabel.text = "친구";
                        favortyScroll.SetActive(false);
                    }
                    else
                    {
                        favortyForm.SetActive(true);
                        friendLabel.text = "친구";
                        favortyScroll.SetActive(true);
                    }

                    friendInfo.RefreshFriendView();

                    UIUtil.CloseWnd<LobbyMainWnd>();
                    UIUtil.CloseStopWnd();
                }));
            }
            else
            {
                UIUtil.OpenStopWnd();

                StartCoroutine(_wait(() =>
                {
                    friendInfo.RefreshFriendList<st_UserToggleData>(
                        Favorites,
                        eUserListType.FavoryFixList,
                        new eUserType[] { eUserType.Favori },
                        new eUserRelation[] { eUserRelation.Friend }, UIUtil.GetWnd<ContactWnd>().SortFavoryFriendList);

                    //친구목록 갱신
                    friendInfo.RefreshFriendList<st_UserToggleData>(Friend, eUserListType.FavoryFixSimpleList,
                            new eUserType[] { eUserType.Normal, eUserType.Favori }, new eUserRelation[] { eUserRelation.Friend }
                            , UIUtil.GetWnd<ContactWnd>().SortFriendList);

                    auto.Init(friendInfo.friendList[Friend]);
                    originIndexs = friendInfo.friendList[Friend].userIndexs;

                    OnChangeInput(searchWord);

                    UIUtil.CloseWnd<LobbyMainWnd>();
                    UIUtil.CloseStopWnd();
                }));
            }
        }

        public override void OnClick_Back()
        {
            if (UIUtil.IsSwiping())
                return;

            UIUtil.Swipe(eWindow.ContactWnd);
        }

        public void OnChangeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                searchMode = false;
                if (0 == friendInfo.uiDataList[Favorites].Count)
                {
                    favortyForm.SetActive(false);
                    favortyScroll.SetActive(false);
                    friendLabel.text = "친구";
                    firendNumber.SetActive(true);
                }
                else
                {
                    favortyForm.SetActive(true);
                    favortyScroll.SetActive(true);
                    friendLabel.text = "친구";
                    firendNumber.SetActive(true);
                }

                friendInfo.RefreshFriendList<st_UserToggleData>(Friend, originIndexs, false, UIUtil.GetWnd<ContactWnd>().SortFriendList);

                friendInfo.RefreshFriendList<st_UserToggleData>(
                    Favorites,
                    eUserListType.FavoryFixList,
                    new eUserType[] { eUserType.Favori },
                    new eUserRelation[] { eUserRelation.Friend }, UIUtil.GetWnd<ContactWnd>().SortFavoryFriendList);

                friendInfo.RefreshFriendView();
            }
            else
            {
                favortyForm.SetActive(false);
                favortyScroll.SetActive(false);
                firendNumber.SetActive(false);
                friendLabel.text = "검색결과";
                searchWord = input;
                searchMode = true;

                auto.MakeSearchList(input);
                List<int> list = new List<int>();

                try
                {
                    for (int i = 0, max = auto.resultSearch.Count; i < max; i++)
                        list.Add(auto.resultSearch[i].index);//originIndexs[auto.resultSearch[i].index]);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                friendInfo.RefreshFriendList<st_UserToggleData>(Friend, list.ToArray(), false, UIUtil.GetWnd<ContactWnd>().SortFriendList);
                friendInfo.RefreshFriendView();
            }
        }

        public void _Process_ReleaseFavori(UserInfo uInfo)
        {
            MyInfo.instance.userinfoMgr.ChangeUserType(uInfo.data.xid, uInfo.userType, eUserType.Normal);

            uInfo.favoryIndex = UIUtil.GetWnd<ContactWnd>().Procrss_CalFavory(false);

            DBUtil.UserInfo_SaveData(uInfo);

            friendInfo.gridInfo[Favorites].grid.RefreshUIObjects();

            RefreshWnd();
        }

        public void OnClick_HideButton()
        {
            long[] f_xidList = SelectList();
            string Nickname = string.Empty;
            bool succes = false;
            for (int i = 0; i < f_xidList.Length; ++i)
            {
                var uInfo = MyInfo.instance.userinfoMgr.GetUserInfo(f_xidList[i]);

                if (uInfo != null)
                {
                    if (eUserType.Favori == uInfo.userType)
                    {
                        MyInfo.instance.userinfoMgr.ChangeUserType(uInfo.data.xid, uInfo.userType, eUserType.Hide);
                        uInfo.favoryIndex = UIUtil.GetWnd<ContactWnd>().Procrss_CalFavory(false);
                    }
                    else
                    {
                        MyInfo.instance.userinfoMgr.ChangeUserType(uInfo.data.xid, uInfo.userType, eUserType.Hide);
                    }

                    if (i == 0)
                    {
                        Nickname += uInfo.data.nickname;
                    }

                    if (i == f_xidList.Length - 1)
                    {
                        succes = true;
                    }
                }
                else
                {
                    UIUtil.ShowMessageBox(string.Format("숨김을 실패 하였습니다\n잠시 후 다시 시도해 주세요"));
                }

                if (succes)
                {
                    if (f_xidList.Length == 1)
                    {
                        UIUtil.ShowMessageBox(Nickname + "님을 숨김 하였습니다");
                    }
                    else
                    {
                        UIUtil.ShowMessageBox(Nickname + String.Format("님 외 {0}명을 숨김 하였습니다", f_xidList.Length - 1));
                    }
                }
            }

            RefreshWnd();
        }

        public void OnClick_BlockButton()
        {
            long[] f_xidList = SelectList();

            StartCoroutine(BlockList(f_xidList));
        }

        private IEnumerator BlockList(long[] f_xidList)
        {
            UIUtil.OpenStopWnd(true);

            for (int i = 0; i < f_xidList.Length; i++)
            {
                yield return StartCoroutine(WebProtocols.instance.OnSend_members_friends_blocks_add(MyInfo.instance.accessSession, f_xidList[i],
                    (bool bSuccess) =>
                    {
                        var uInfo = MyInfo.instance.userinfoMgr.GetUserInfo(f_xidList[i]);
                        if (uInfo == null)
                        {
                            UIUtil.ShowMessageBox("해당 유저가 리스트에 존재하지 않습니다");
                        }
                        else
                        {
                            if (bSuccess)
                            {
                                if (eUserType.Favori == uInfo.userType)
                                {
                                    MyInfo.instance.userinfoMgr.ChangeUserType(uInfo.data.xid, uInfo.userType, eUserType.Block);
                                    uInfo.favoryIndex = UIUtil.GetWnd<ContactWnd>().Procrss_CalFavory(false);
                                }
                                else
                                {
                                    MyInfo.instance.userinfoMgr.ChangeUserType(uInfo.data.xid, uInfo.userType, eUserType.Block);
                                }

                                UIUtil.ShowMessageBox(string.Format("{0}님을 차단 하였습니다", uInfo.data.nickname));                                
                                UIManager.instance.swipeSystem.Current.RefreshWnd();
                            }
                            else
                            {
                                UIUtil.ShowMessageBox(string.Format("{0}님의 차단을 실패 하였습니다\n잠시 후 다시 시도해 주세요", uInfo.data.nickname));
                            }
                        }
                    }));
            }

            UIUtil.CloseStopWnd();

            //RefreshWnd();

            yield return StartCoroutine(GetFriendList((bool bSuccess) =>
            {
                SearchInput.value = "";
                friendLabel.text = "친구";
                firendNumber.SetActive(true);
                // 롱터치 초기화
                //friendInfo.InitLongTouch("", this);

                // 친구 목록 초기화
                friendInfo.InitUserList<st_UserToggleData>(Favorites,
                    eUserListType.FavoryFixList, eUserType.Favori, eUserRelation.Friend, UIUtil.GetWnd<ContactWnd>().SortFavoryFriendList);

                // 친구목록 초기화
                friendInfo.InitUserList<st_UserToggleData>(Friend, eUserListType.FavoryFixSimpleList,
                    new eUserType[] { eUserType.Normal, eUserType.Favori }, new eUserRelation[] { eUserRelation.Friend }
                    , UIUtil.GetWnd<ContactWnd>().SortFriendList);

                // 아이디 검색 초기화
                auto.Init(friendInfo.friendList[Friend]);
                originIndexs = friendInfo.friendList[Friend].userIndexs;

                if (0 == friendInfo.uiDataList[Favorites].Count)
                {
                    favortyForm.SetActive(false);
                    favortyScroll.SetActive(false);
                }
                else
                {
                    favortyForm.SetActive(true);
                    favortyScroll.SetActive(true);
                }
                friendInfo.RefreshFriendList<st_UserToggleData>(
                        Favorites,
                        eUserListType.FavoryFixList,
                        new eUserType[] { eUserType.Favori },
                        new eUserRelation[] { eUserRelation.Friend }, UIUtil.GetWnd<ContactWnd>().SortFavoryFriendList);

                //친구목록 갱신
                friendInfo.RefreshFriendList<st_UserToggleData>(Friend, eUserListType.FavoryFixSimpleList,
                    new eUserType[] { eUserType.Normal, eUserType.Favori }, new eUserRelation[] { eUserRelation.Friend }
                    , UIUtil.GetWnd<ContactWnd>().SortFriendList);

                friendInfo.RefreshFriendView();
                UIUtil.CloseStopWnd();

                UIUtil.CloseWnd<LobbyMainWnd>();
            }));
        }

        private void BlockRefresh()
        {
            StartCoroutine(GetFriendList((bool bSuccess) =>
            {
                SearchInput.value = "";
                friendLabel.text = "친구";
                firendNumber.SetActive(true);
                // 롱터치 초기화
                //friendInfo.InitLongTouch("", this);

                // 친구 목록 초기화
                friendInfo.InitUserList<st_UserToggleData>(Favorites,
                    eUserListType.FavoryFixList, eUserType.Favori, eUserRelation.Friend, UIUtil.GetWnd<ContactWnd>().SortFavoryFriendList);

                // 친구목록 초기화
                friendInfo.InitUserList<st_UserToggleData>(Friend, eUserListType.FavoryFixSimpleList,
                    new eUserType[] { eUserType.Normal, eUserType.Favori }, new eUserRelation[] { eUserRelation.Friend },
                    UIUtil.GetWnd<ContactWnd>().SortFriendList);

                // 아이디 검색 초기화
                auto.Init(friendInfo.friendList[Friend]);
                originIndexs = friendInfo.friendList[Friend].userIndexs;

                if (0 == friendInfo.uiDataList[Favorites].Count)
                {
                    favortyForm.SetActive(false);
                    favortyScroll.SetActive(false);
                }
                else
                {
                    favortyForm.SetActive(true);
                    favortyScroll.SetActive(true);
                }
                friendInfo.RefreshFriendList<st_UserToggleData>(
                        Favorites,
                        eUserListType.FavoryFixList,
                        new eUserType[] { eUserType.Favori },
                        new eUserRelation[] { eUserRelation.Friend }, UIUtil.GetWnd<ContactWnd>().SortFavoryFriendList);

                //친구목록 갱신
                friendInfo.RefreshFriendList<st_UserToggleData>(Friend, eUserListType.FavoryFixSimpleList,
                    new eUserType[] { eUserType.Normal, eUserType.Favori }, new eUserRelation[] { eUserRelation.Friend }
                    , UIUtil.GetWnd<ContactWnd>().SortFriendList);

                friendInfo.RefreshFriendView();
                UIUtil.CloseStopWnd();

                UIUtil.CloseWnd<LobbyMainWnd>();
            }));
        }

        private long[] SelectList()
        {
            var resultList = friendInfo.uiDataList[Friend].FindAll((st_UserBarData item) =>
            {
                //  선택한 유저들이 아니면 false
                bool toggle = (item as st_UserToggleData).toggle;
                if (!toggle)
                    return false;

                return true;

            });

            return resultList.ConvertAll<long>((st_UserBarData item) => { return MyInfo.instance.userinfoMgr.userStore[item.storeIndex].data.xid; }).ToArray();
        }

        private IEnumerator GetFriendList(Action<bool> onComplete)
        {
            bool flag = false;

            yield return StartCoroutine(WebProtocols.instance.GetFriendList(MyInfo.instance.accessSession, MyInfo.instance.rev, (bool bSuccess) =>
            {
                flag = bSuccess;

            }));

            yield return StartCoroutine(WebProtocols.instance.GetRequestingUserList(MyInfo.instance.accessSession, (bool bSuccess) =>
            {
                flag = bSuccess;
            }));

            onComplete(true);
        }
    }
}
