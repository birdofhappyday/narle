using UnityEngine;
using Assets.Scripts.Utils;
using Assets.Scripts.Cores;
using System.Collections.Generic;
using Assets.Scripts.User;
using Core.UI;
using System.Collections;
using Assets.Scripts.Cores.LocalDB;
using Assets.Scripts.Networks.Web;
using System;
using Assets.Scripts.UI.Windows.Popup;
using Assets.Scripts.UI.Windows.Lobby.OpenChat;
using Assets.Scripts.Messenger;
using System.Linq;
using Assets.Scripts.UI.Windows.Lobby.VideoCallWindow;

namespace Assets.Scripts.UI.Windows.Lobby
{
    public class OpenChatWnd : UISwipeWndBase
    {
        #region 사용 클래스

        public class BeforeSearchInputString
        {
            public string msearchString = string.Empty;
            public string msearchDate = string.Empty;

            public BeforeSearchInputString()
            {
            }

            public BeforeSearchInputString(string searchString, string searchDate)
            {
                msearchString = searchString;
                msearchDate = searchDate;
            }
        }

        #endregion

        #region 프로필 관련

        public enum ProfileImage
        {
            RealPhoto,
            Avata,
        }

        public UILabel openChatNickname;
        public UILabel openChatHeartNumber;
        public UILabel opebChatHeartAutoMaxNumber;
        public Texture2D m_basicProfileImage;

        // [gh040103] 프로필 이미지 처리.
        public UITexture myprofile; // 이미지로만 됨.
        public UITexture nonPhoto;

        private const int Max_AUTOHEART_MAXNUMBER = 10;

        #endregion

        #region 스크롤뷰
        public HM_ScrollViewEX openChatRoomScrollEX;
        public HM_ScrollGridEX recommandlOpenRoomScrollGribEX;
        public HM_ScrollGridEX recentlyOpenRoomScrollGribEX;
        public UITable tagUITable;
        public HM_ScrollViewEX beforeSearchScrollEX;
        public HM_ScrollGridEX beforeSeacrchGribEX;
        public HM_ScrollViewEX searchResultScrollEX;
        public HM_ScrollGridEX searchResultGribEX;
        public HM_ScrollViewEX filiterResultScrollEX;
        public HM_ScrollGridEX filiterResultGribEX;

        // [gh042006] 리스트 비어도 드래그 되도록
        public GameObject dragObject;

        [HideInInspector]
        public UIPanel openChatRoomScrollEXPanel;
        [HideInInspector]
        public UIPanel searchResultScrollEXPanel;

        public GameObject recommandBG;
        public GameObject recentBG;
        [HideInInspector]
        public bool openChatWndRefresh = true;
        #endregion

        #region Search 관련

        public UIInput searchInput;
        public GameObject emptySearchResultObject;
        public GameObject searchFilterOption;
        public GameObject beforeSearchRemoveButton;
        public GameObject[] searchFristFilterOption;
        public GameObject[] searchSecondFilterOption;
        public GameObject[] openChatTagInfo;
        public GameObject openChatTag;
        public UIToggle matchingButton;
        public GameObject matchingOutline;

        private bool searchFirstWrite = true;
        public bool matchingActive = false; // [gh041911] 매칭 처리용.

        #endregion

        #region OpenChatSetting관련 변수

        public enum OpenChatMode
        {
            Basic,
            Search,
        }

        public GameObject openChatRoomCreatButton;
        public OpenChatSpringPanel openChatSpringPanel;
        [HideInInspector]
        public OpenChatMode openChatMode = OpenChatMode.Basic;

        #endregion

        // [gh042507] 빈공간 드래그용.
        public GameObject[] dragBlanks;

        // [gh050303] 검색어 표시 요청 977, 979
        public UILabel searchText;
        // [gh050403] 검색어 배경 동그라미 표시 요청.
        public GameObject searchBg;
   
        // [gh041811] 경고 변경.
        #region Contacts & overray 경고
        public GameObject contact_alert, overray_alert;
        private enum ALERT_TYPE
        {
            NONE = 0,
            CONTACTS,
            OVERRAY,
            END
        }

        private ALERT_TYPE alert_type;
        private bool isCheckAlert = false;
        // [gh042007] 오버레이 권한 이동 변경.
        private IEnumerator CheckAlert()
        {
            isCheckAlert = true;
            if (alert_type == ALERT_TYPE.CONTACTS)
            {
                GHPermission.RequestPermission();

                while (!ScriptMgr.instance.Get<AndroidPlugin>().U2P_CheckPermission(new eRequestPermission[] { eRequestPermission.CONTACTS }))
                {
                    yield return new WaitForSeconds(0.1f);
                }
                contact_alert.SetActive(false);

                /*if (!ScriptMgr.instance.Get<AndroidPlugin>().U2P_CheckObtainingPermissionOverlayWindow())
                {
                    overray_alert.SetActive(true);
                }
                else
                    overray_alert.SetActive(false);*/
            }
            else if (alert_type == ALERT_TYPE.OVERRAY)
            {
                GHPermission.RequestPermissionOveray();

                while (!ScriptMgr.instance.Get<AndroidPlugin>().U2P_CheckObtainingPermissionOverlayWindow())
                {
                    yield return new WaitForSeconds(0.1f);
                }
                overray_alert.SetActive(false);
            }
            isCheckAlert = false;
        }

        public void OnClickOverrayAlert()
        {
            alert_type = ALERT_TYPE.OVERRAY;
            StartCoroutine(CheckAlert());
        }

        public void OnClickContactAlert()
        {
            alert_type = ALERT_TYPE.CONTACTS;
            StartCoroutine(CheckAlert());
        }

        private void OnDisable()
        {
            if (isCheckAlert)
            {
                StopCoroutine(CheckAlert());
                isCheckAlert = false;
            }

            // [gh041406] 화면 삭제시 혹시라도 처리.
            UIUtil.CloseStopWnd();

            // [gh050903] 전화가 오거나 하면 팝업 제거용.
            UIPopupWndBase checkWnd = UIUtil.GetWnd<POP_OpenChat_MyChatname>(false);
            if(checkWnd && checkWnd.IsVisible())
            {
                checkWnd.Close();
            }

            checkWnd = UIUtil.GetWnd<POP_OpenChat_Waiting>(false);
            if (checkWnd && checkWnd.IsVisible())
            {
                checkWnd.Close();
            }

            //Debug.unityLogger.logEnabled = false;
        }
        // end
        #endregion

        #region PUSH 처리용. [gh]
        public GameObject wait_alert;
        public GameObject wait_hide_obj;
        // 방으로 들어가기 위한 처리. ui위주.
        // [gh042405] 부모지정으로 어디든 표시 될 수 있게 변경.
        public void SetWaitRoomJoin(bool isWait, Transform papa = null)
        {
            if (isWait)
            {
                if (papa != null)
                {
                    wait_alert.transform.SetParent(papa);
                    wait_alert.transform.localPosition = Vector3.zero;
                }
                wait_alert.SetActive(true);
                wait_hide_obj.SetActive(false);
            }
            else // 안보일 때는 원복한다.
            {
                wait_alert.transform.SetParent(transform);
                wait_alert.transform.localPosition = Vector3.zero;
                wait_alert.SetActive(false);
                wait_hide_obj.SetActive(true);
            }
        }

        public bool isWaitRoomJoin()
        {
            return wait_alert.activeSelf;
        }

        #endregion

        #region BasicSetting

        private void Awake()
        {
            Debug.Log(name + ":" + transform.parent);
            IsDontDestroy = true;
            openChatRoomScrollEX.KSM_CancelDragIfFits = true;
            searchResultScrollEX.KSM_CancelDragIfFits = true;
            openChatRoomScrollEXPanel = openChatRoomScrollEX.gameObject.GetComponent<UIPanel>();
            searchResultScrollEXPanel = searchResultScrollEX.gameObject.GetComponent<UIPanel>();

            // add [gh0329] 일단 하트 저장한거 부른다. 화면 갱신 전까지만.
            //MyInfo.instance.UserData.heart = PlayerPrefs.GetInt("GHheart", 0);
            /*UIUtil.ShowMessageBox("유튜브 같이 보기를 하기 위해서는 권한 설정이 필요합니다.", "유튜브", "취소", () => { }, "설정하기", () =>
            {
                GHPermission.RequestPermissionOveray();
            });*/
            // OnHeartCount();
        }

        public override void Close(params object[] args)
        {
            base.Close(args);
            dragObject.SetActive(false);
        }

        public override void Open(params object[] args)
        {
            base.Open(args);
            //Debug.unityLogger.logEnabled = true;
            // [gh041205] 푸시로 온거면 무시한다.
            if (Test_Firebase.isTaskPosh)
            {
                if (openChatMode == OpenChatMode.Basic)
                {
                    matchingActive = matchingButton.value;
                    matchingOutline.SetActive(matchingActive);
                    OpenChatBasicRoom();
                    InitOpenChatProfile();
                }
                else
                {
                    matchingActive = matchingButton.value;
                    string beforeWord = searchInput.value;
                    OnChange_OpenChatroomSearchMode();
                    searchInput.value = beforeWord;
                    matchingOutline.SetActive(matchingActive);
                    if (searchFirstWrite)
                    {
                        SearchOpenChat(beforeWord, false);
                    }
                }

                ScriptMgr.instance.Get<Avatars.AvatarManager>().ExitAvartarMode();
                ScriptMgr.instance.Get<LoginManager>().CheckSessionTime();
                return;
            }

            // [gh051013] 1103 관련처리.
            //if (RoomManager.OpenChatRoomDic.Count > 0)
            {
                // [gh051507] 리스트 제거 및 갱신처리.
                RoomManager.OpenChatRoomDic.Clear();
                UIUtil.OpenStopWnd(true);
                recentlyOpenRoomScrollGribEX.Refresh(0);
                recommandlOpenRoomScrollGribEX.Refresh(0);
                isSkipReqList = false;
            }

            StartCoroutine(OpenChatStart((bool bSuccess, bool limit) =>
            {
                if (limit)
                {
                    DateTime sendDate = Util.GetUnixTimeStampToDateTime(MyInfo.instance.UserData.until_date);
                    UIUtil.ShowMessageBox("이 계정은 신고 누적으로 인해서 \n" + sendDate.Date.ToString("yyyy-MM-dd") + "까지 오픈플래닛 \n이용이 제한되었습니다.\n" +
                        "상세 정보를 확인하고 싶으시면 \n고객센터에서 확인하시기 바랍니다.", "", "확인", () => { UIUtil.GetWnd<LobbyMainWnd>().OnClick_Beauty(); });
                    UIUtil.GetWnd<POP_MessageBox>().UIMessage.fontSize = 42;
                    UIUtil.CloseStopWnd();
                    return;
                }

                if (openChatMode == OpenChatMode.Basic)
                {
                    matchingActive = matchingButton.value;
                    matchingOutline.SetActive(matchingActive);
                    OpenChatBasicRoom();
                    InitOpenChatProfile();
                }
                else
                {
                    matchingActive = matchingButton.value;
                    string beforeWord = searchInput.value;
                    // [gh050905] 서치 모드시 서치된 리스트 있으면 재 갱신 처리한다.
                    if (beforeWord.Length > 0)
                    {
                        SearchOpenChat(beforeWord, true);
                        needSearchSelected = false;
                    }
                    else
                    {
                        OnChange_OpenChatroomSearchMode();
                        searchInput.value = beforeWord;
                        matchingOutline.SetActive(matchingActive);
                        if (searchFirstWrite)
                        {
                            SearchOpenChat(beforeWord, false);
                        }
                    }
                }

                UIUtil.OpenWnd<LobbyMainWnd>();
                UIUtil.CloseStopWnd();
                if (bSuccess)
                {
                    FirstVisitOpenChatWnd();
                }
            }));


            if (GHPermission.Check("android.permission.READ_CONTACTS") == GHPermission.GRANT_TYPE.DENY)
            {
                Debug.Log("gh:READ_CONTACTS deny");
                contact_alert.SetActive(true);
            }
            // [gh042408] 오버레이 권한 체크 이동. 방생성 및 입장 때로.
            /*else
            {
                contact_alert.SetActive(false);
                if (!ScriptMgr.instance.Get<AndroidPlugin>().U2P_CheckObtainingPermissionOverlayWindow())
                {
                    overray_alert.SetActive(true);
                }
                else
                    overray_alert.SetActive(false);
            }*/
        }

        // [gh051008] 1105 관련하여 리스트 갱신을 스킵하기 위한 용도.
        public bool isSkipReqList = false;
        
        // [gh051011] 1105 연관.
        public IEnumerator OnReqTagList(Action cb)
        {
            tagUITable.Reposition();
            yield return StartCoroutine(OnSend_OpenChatTagList((bool bSuccess) =>
            {
                if (bSuccess)
                {
                    for (int i = 0; i < openChatTagInfo.Length; i++)
                    {
                        if (openChatTagInfo != null)
                            Destroy(openChatTagInfo[i]);
                    }

                    for (int i = 0; i < RoomManager.OpenChatTagList.Count; ++i)
                    {
                        if (openChatTagInfo[i] != null)
                        {
                            Destroy(openChatTagInfo[i]);
                        }
                        var tagObject = Instantiate(openChatTag);
                        openChatTagInfo[i] = tagObject;
                        tagObject.transform.SetParent(tagUITable.transform);
                        tagObject.transform.localScale = Vector3.one;
                        openChatTagInfo[i].SetActive(false);
                        tagObject.GetComponent<OpenChat_tag>().SetData(RoomManager.OpenChatTagList[i].tagName);

                        openChatTagInfo[i].SetActive(true);
                    }

                    for (int i = RoomManager.OpenChatTagList.Count; i < 10; ++i)
                    {
                        if (openChatTagInfo[i] == null)
                        {
                            break;
                        }
                        Destroy(openChatTagInfo[i]);
                    }
                }
            }));
            Debug.Log("GH-----------------------------");
            yield return StartCoroutine(_wait(() =>
            {
                tagUITable.Reposition();
                tagUITable.repositionNow = true;
            }));

            cb();
        }

        /// <summary>
		/// RefreshWnd() : UI 화면 갱신
		/// </summary>
		public override void RefreshWnd(params object[] args)
        {
            bool flag = false;
            bool bInit = false;
            if (Test_Firebase.isTaskPosh)
            {
                return;
            }

            try
            {
                bInit = (bool)args[0];
            }
            catch
            {
            }

            Debug.Log("오픈플래닛 갱신!!!!!!!!!!!!!!!");

            if (bInit)
            {
                // [gh042004] 오픈플래닛에서 미니게임 UI 가리기
                var mGameUI = GameObject.FindObjectOfType<MiniGameManager>();
                if (mGameUI && mGameUI.GetComponent<UIPanel>())
                {
                    if (mGameUI.gameObject.activeSelf)
                    {
                        mGameUI.gameObject.SetActive(false);
                    }
                }
                // end

                openChatWndRefresh = true;
                openChatRoomScrollEXPanel.KSM_SpringPanelEndActive = false;
                searchResultScrollEXPanel.KSM_SpringPanelEndActive = false;

                //[gh051009] 리스트 갱신 스킵.
                if (!isSkipReqList)
                {
                    UIUtil.OpenStopWnd(true);
                    StartCoroutine(OnSend_OpenChatRoomListandTagList((bool bSuccess) =>
                    {
                        flag = bSuccess;

                        if (!bSuccess)
                        {
                            Debug.LogError("오픈플래닛 갱신실패");
                        }

                        UIUtil.CloseStopWnd();
                    }));
                }
            }
            else
            {
                if (openChatWndRefresh)
                {
                    //[gh051009] 리스트 갱신 포기.
                    if (!isSkipReqList)
                    {
                        UIUtil.OpenStopWnd(true);
                        StartCoroutine(OnSend_AddOpenChatRoomList((bool bSuccess) =>
                        {
                            flag = bSuccess;

                            if (!bSuccess)
                            {
                                UIUtil.ShowMessageBox("플래닛 목록 조회(refrehsh) 실패");
                                return;
                            }

                            UIUtil.CloseStopWnd();
                        }));
                    }
                }
                else
                {
                    UIUtil.CloseStopWnd();
                }
            }

            isSkipReqList = false;
        }

        public override void RecoverWnd()
        {
            base.RecoverWnd();

            if (openChatMode == OpenChatMode.Basic)
            {
                matchingActive = matchingButton.value;
                matchingOutline.SetActive(matchingActive);
                OpenChatBasicRoom();
                InitOpenChatProfile();
            }
            else
            {
                matchingActive = matchingButton.value;
                string beforeWord = searchInput.value;
                OnChange_OpenChatroomSearchMode();
                searchInput.value = beforeWord;
                matchingOutline.SetActive(matchingActive);
                if (searchFirstWrite)
                {
                    SearchOpenChat(beforeWord, false);
                }
            }

            UIUtil.OpenWnd<LobbyMainWnd>();
        }

        public void OfflineRefresh()
        {
            var recommandRoomList = RoomManager.OpenChatRoomDic.Values.ToList().Where(x => x.OpenChatrecommand).ToList();

            if (recommandRoomList.Count == 0)
            {
                recommandBG.SetActive(false);
                recommandlOpenRoomScrollGribEX.Refresh(recommandRoomList.Count, false, recommandRoomList);
            }
            else
            {
                recommandBG.SetActive(true);
                recommandlOpenRoomScrollGribEX.Refresh(recommandRoomList.Count, false, recommandRoomList);
            }

            // [gh041405] 추천만 올 수 있으니 변경처리.
            var recentlyRoomList = RoomManager.OpenChatRoomDic.Values.ToList().Where(x => !x.OpenChatrecommand).ToList();

            if (recentlyRoomList.Count == 0)
            {
                recentBG.SetActive(false);
                recentlyOpenRoomScrollGribEX.Refresh(recentlyRoomList.Count, false, recentlyRoomList);
            }
            else
            {
                recentBG.SetActive(true);
                recentlyOpenRoomScrollGribEX.Refresh(recentlyRoomList.Count, false, recentlyRoomList);
            }

            openChatRoomScrollEX.Refresh();
        }

        private IEnumerator _wait(Action callback)
        {
            yield return null;
            if (callback != null)
            {
                callback();
            }
        }

        private IEnumerator OpenChatStart(Action<bool, bool> callback)
        {
            bool flag = false;
            bool limit = false;

            yield return WebProcessor.instance.StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_MatchingCheck((bool bSuccess) =>
            {
                flag = bSuccess;
            }));

            if (!flag)
            {
                UIUtil.ShowMessageBox("매칭 조회에 실패했습니다. 잠시 후 다시 시도해주세요.");
                Debug.LogError(WebProcessor.instance.RecvInfo.error_code);

                callback(flag, limit);
                yield break;
            }

            if (!Test_Firebase.isTaskPosh)
                OnReqHeart();

            yield return WebProcessor.instance.StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_Config((bool bSuccess) =>
            {
                //flag = bSuccess;
                // [gh041614] 닉네임 처리.
                if (bSuccess)
                {
                    this.openChatNickname.text = MyInfo.instance.UserData.OpenChatNickName;
                    limit = MyInfo.instance.UserData.isEnterLimited;
                }
            }));

            yield return null;
            callback(flag, limit);
        }
        #endregion

        #region PUSH : 푸시 연관 처리.
        /// <summary>
        /// [gh041210] 푸시로 로그인까지 한 경우.
        /// </summary>
        /// 

        public IEnumerator OnPushRoom(Action<bool> callback)
        {
            yield return WebProcessor.instance.StartCoroutine(OpenChatStart((bool bSuccess, bool limit) =>
            {
                if (limit)
                {
                    DateTime sendDate = Util.GetUnixTimeStampToDateTime(MyInfo.instance.UserData.until_date);
                    UIUtil.ShowMessageBox("이 계정은 신고 누적으로 인해서 \n" + sendDate.Date.ToString("yyyy-MM-dd") + "까지 오픈플래닛 \n이용이 제한되었습니다.\n" +
                        "상세 정보를 확인하고 싶으시면 \n고객센터에서 확인하시기 바랍니다.", "", "확인", () => { UIUtil.GetWnd<LobbyMainWnd>().OnClick_Beauty(); });
                    UIUtil.GetWnd<POP_MessageBox>().UIMessage.fontSize = 42;
                    bSuccess = false;
                }

                if (bSuccess)
                {
                    if (openChatMode == OpenChatMode.Basic)
                    {
                        matchingActive = matchingButton.value;
                        matchingOutline.SetActive(matchingActive);
                        OpenChatBasicRoom();
                        InitOpenChatProfile();
                    }
                    else
                    {
                        matchingActive = matchingButton.value;
                        string beforeWord = searchInput.value;
                        OnChange_OpenChatroomSearchMode();
                        searchInput.value = beforeWord;
                        matchingOutline.SetActive(matchingActive);
                        if (searchFirstWrite)
                        {
                            SearchOpenChat(beforeWord, false);
                        }
                    }
                    UIUtil.OpenWnd<LobbyMainWnd>();
                }

                callback(bSuccess);
            }));
        }
        #endregion

        #region BasicFunction

        /// <summary>
        /// OpenChatWnd를 처음 열음.
        /// </summary>
        private void FirstVisitOpenChatWnd()
        {
            if (MyInfo.instance.UserData.isOpenChatRoomFirstVisit)  // 처음이다.
            {
                UIUtil.OpenWnd<POP_OpenChat_Welcome>();

                MyInfo.instance.UserData.isOpenChatRoomFirstVisit = false;
                DBUtil.MyInfo_SaveData(MyInfo.instance);
            }
        }

        /// <summary>
        /// 기본 모드 열기.
        /// </summary>
        private void OpenChatBasicRoom()
        {
            for (int i = 0; i < this.childList.Length; ++i)
            {
                this.childList[i].gameObject.SetActive(false);
            }

            this.childList[0].gameObject.SetActive(true);
            openChatRoomCreatButton.SetActive(true);
            beforeSearchScrollEX.gameObject.SetActive(false);
            openChatMode = OpenChatMode.Basic;
            // [gh051011] 1105관련.
            if (!isSkipReqList)
            {
                recommandBG.SetActive(false);
                recentBG.SetActive(false);
            }

            // [gh050304] 매칭 키워드 바로 보여주기.
            // [gh050706] 매칭 일 경우만.
            if (matchingActive || searchInput.value.Length>0)
            {
                searchText.text = searchInput.value;
                searchBg.SetActive(true);
            }
            else
            {
                searchText.text = "검색어를 입력하세요";
                searchBg.SetActive(false);
            }

            RefreshWnd(true);
        }

        // [gh050708] 클리어 추가.
        public void OnClickClearInput()
        {
            searchInput.value = "";
            if (matchingActive)
            {
                WebProtocols.instance.StartCoroutine(OnSend_MatchingToggleDective((bool bSuccess) =>
                {
                    if (bSuccess)
                    {
                        matchingActive = false;
                        matchingButton.value = false;
                        searchInput.value = "";
                        matchingOutline.SetActive(matchingActive);
                    }
                }));
            }
        }

        /// <summary>
        /// 검색 모드 열기.
        /// </summary>
        private void OpenChatSearchRoom()
        {
            for (int i = 0; i < this.childList.Length; ++i)
            {
                this.childList[i].gameObject.SetActive(false);
            }

            // [gh050905] 키워드 기록을 남기를 쪽으로 수정됨.
            //if (!matchingActive)
            //{
            //    searchInput.value = "";
            //}
            this.childList[1].gameObject.SetActive(true);
            searchResultGribEX.gameObject.SetActive(false);
            beforeSearchScrollEX.gameObject.SetActive(true);
            emptySearchResultObject.SetActive(false);
            openChatRoomCreatButton.SetActive(false);
            beforeSearchRemoveButton.SetActive(false);
            beforeSeacrchGribEX.gameObject.SetActive(false);
            InitSearchFiliterOption();
            searchFilterOption.SetActive(false);
            searchResultScrollEX.gameObject.SetActive(false);

            if (0 != MyInfo.instance.UserData.beforeSearchInputStrings.Count)
            {
                beforeSearchRemoveButton.SetActive(true);
                beforeSeacrchGribEX.gameObject.SetActive(true);
                MyInfo.instance.UserData.beforeSearchInputStrings.Reverse();
                beforeSeacrchGribEX.Refresh(MyInfo.instance.UserData.beforeSearchInputStrings.Count, true ,MyInfo.instance.UserData.beforeSearchInputStrings);
                beforeSearchScrollEX.Refresh();
                MyInfo.instance.UserData.beforeSearchInputStrings.Reverse();
                ///위치가 안 맞을 수 있어 한 프레임뒤 정렬(바로 하면 시점이 안 맞을 수 있어서 한 프레임 뒤에 한다.)
                StartCoroutine(_wait(() =>
                {
                    beforeSearchScrollEX.Init();
                }));
            }
            openChatMode = OpenChatMode.Search;
        }

        #endregion

        #region Init

        /// <summary>
        /// 프로필 상태 초기화
        /// </summary>
        public void InitOpenChatProfile()
        {
            //[gh041616] 닉네임 오픈 채팅용으로.
            if (string.IsNullOrEmpty(MyInfo.instance.UserData.OpenChatNickName))
                MyInfo.instance.UserData.OpenChatNickName = MyInfo.instance.UserData.nickname;

            openChatNickname.text = MyInfo.instance.UserData.OpenChatNickName;
            openChatNickname.width = (int)openChatNickname.printedSize.x;
            //opebChatHeartAutoMaxNumber.text = "/" + Max_AUTOHEART_MAXNUMBER.ToString();

            // [gh040104] 이미지 표시.
            // [gh050801] 기본 이미지 인 경우 다시 복구.
            if(MyInfo.instance.UserData.profileTexture == null && MyInfo.instance.UserData.isOpenChatRoomProfileAllow == true)
            {
                nonPhoto.gameObject.SetActive(true);
                myprofile.gameObject.SetActive(false);
            }
            else if (MyInfo.instance.UserData.profileTexture == null || MyInfo.instance.UserData.isOpenChatRoomProfileAllow == false)
            {
                myprofile.mainTexture = null;
                myprofile.material = UIUtil.SetProfilePhotoMaterial(GHGlobal.H.ipCharTexRect[UnityEngine.Random.Range(0, GHGlobal.H.ipCharTexRect.Length)]);
                // [gh051512] 비공개인 경우 이미지 복구.
                myprofile.gameObject.SetActive(true);
                nonPhoto.gameObject.SetActive(false);
            }
            else
            {
                nonPhoto.gameObject.SetActive(false);
                myprofile.gameObject.SetActive(true);
                myprofile.material = UIUtil.SetProfilePhotoMaterial(MyInfo.instance.UserData.profileTexture);
                //myprofile.mainTexture = MyInfo.instance.UserData.profileTexture;
            }
            //OpenChatHeartNumber.text = "12";                        //하트 개수 넣을 수 있도록 추가.
            //OpenChatHeartNumber.AssumeNaturalSize();
        }

        /// <summary>
        /// InitSearchFiliterOption : 검색 필터 오브젝트 끄기.
        /// </summary>
        private void InitSearchFiliterOption()
        {
            for (int i = 0; i < searchFristFilterOption.Length; ++i)
            {
                searchFristFilterOption[i].SetActive(false);
            }
            for (int i = 0; i < searchSecondFilterOption.Length; ++i)
            {
                searchSecondFilterOption[i].SetActive(false);
            }
        }

        /// <summary>
        /// InitSearchFiliterOption : 첫번째 검색 필터 오브젝트 끄기.
        /// </summary>
        private void InitSearchFirstFiliterOption(bool all = true, int index = 0)
        {
            if (all)
            {
                for (int i = 0; i < searchFristFilterOption.Length; ++i)
                {
                    searchFristFilterOption[i].SetActive(false);
                }
            }
            else
                searchFristFilterOption[index].SetActive(false);
        }

        /// <summary>
        /// InitSearchFiliterOption : 두번째 검색 필터 오브젝트 끄기.
        /// </summary>
        private void InitSearchSecondFiliterOption(bool all = true, int index = 0)
        {
            if (all)
            {
                for (int i = 0; i < searchFristFilterOption.Length; ++i)
                {
                    searchSecondFilterOption[i].SetActive(false);
                }
            }
            else
                searchSecondFilterOption[index].SetActive(false);

            var searchRoomList = RoomManager.OpenChatSearchRoomDic.Values.ToList();
            searchResultGribEX.Init(searchRoomList.Count, searchRoomList);
            searchResultScrollEX.Refresh();
        }

        #endregion

        #region Button

        /// <summary>
        /// 프로필 버튼 클릭.
        /// </summary>
        public void Onclick_ProfileChange()
        {
            //UIUtil.Swipe(eWindow.POP_OpenChat_MyChatname);
            UIUtil.OpenWnd<POP_OpenChat_MyChatname>();
        }

        /// <summary>
        /// 보석 구매 버튼 클릭.
        /// </summary>
        public void Onclick_OpenChat_ChargeJew()
        {
            UIUtil.Swipe(eWindow.OpenChat_ChargeJew);
            UIUtil.CloseWnd<LobbyMainWnd>();
        }

        /// <summary>
        /// 방 생성 UI 클릭.
        /// </summary>
        public void Onclick_CreatOpenChatRoom()
        {
            // [gh061101] 로그 기록.
            PanalyzerUtil.PAN_menuItem("DC", "C", "");

            UIUtil.Swipe(eWindow.OpenChat_CreatRoom);
        }

        /// <summary>
        /// 매칭대기 버튼 직접 클릭
        /// </summary>
        public void Onclick_MatchingButton()
        {
            if (!matchingActive)
            {
                // [gh050206] 오버레이 권한 체크.
                if (!ScriptMgr.instance.Get<AndroidPlugin>().U2P_CheckObtainingPermissionOverlayWindow())
                {
                    UIUtil.ShowMessageBox("다른 사람들과 함께 오픈플래닛을\n사용하려면 다른 앱 위에 표시\n권한을 허용해야 합니다.", "", "취소", () => {
                        matchingButton.value = false;
                    }, "이동", () => {
                        GHPermission.RequestPermissionOveray();
                        matchingButton.value = false;
                    });
                    return;
                }
                //

                UIUtil.OpenWnd(eWindow.POP_OpenChat_SettingSearch);
            }
            else
            {
                WebProtocols.instance.StartCoroutine(OnSend_MatchingToggleDective((bool bSuccess) =>
                {
                    if (bSuccess)
                    {
                        matchingActive = false;
                        matchingButton.value = false;
                        searchInput.value = "";
                        matchingOutline.SetActive(matchingActive);

                        // [gh050304] 매칭 키워드 바로 보여주기.
                        searchText.text = "검색어를 입력하세요";
                    }
                }));
            }
        }

        public void Onclick_OpenChatMemberInfo()
        {
            //OnSend_OpenChatRoomMemberList();
        }

        /// <summary>
        /// [gh0329] 하트 정보를 요청한다.
        /// </summary>
        public void OnReqHeart()
        {
            StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_HeartConfig((bool bSuccess) =>
            {
                // add [gh0329] 성공시 하트 갱신.
                if (bSuccess)
                {
                    OnHeartCount();
                    PlayerPrefs.SetInt("GHheart", MyInfo.instance.UserData.heart);
                    PlayerPrefs.Save();
                }
                else
                {
                    UIUtil.ShowMessageBox("하트 조회 실패");
                }
            }));
        }

        /// <summary>
        /// [gh0329] 하트 갱신한다.
        /// </summary>
        public void OnHeartCount()
        {
            Debug.Log("OnHeart");
            openChatHeartNumber.text = MyInfo.instance.UserData.heart.ToString("D4");
        }

        // [gh050814] 뒤로 가기 처리?
        public override bool OnTouch_Back()
        {
            return false;
        }
        // end

        #endregion

        #region Search

        /// <summary>
        /// 매칭토글 가동시키기.
        /// input에 string값으로 가동
        /// </summary>
        public void Active_MatchingButton(string search = null)
        {
            if (string.IsNullOrEmpty(search))
            {
            }
            else
            {
                StartCoroutine(OnSend_MatchingToggleActive(search, (bool bSuccess) =>
                {
                    if (bSuccess)
                    {
                        matchingButton.value = true;
                        matchingActive = true;
                        matchingOutline.SetActive(matchingActive);

                        //[gh050710] 매칭 키워드 넣어야 한다.
                        searchInput.value = search;
                    }
                }));
            }
        }

        /// <summary>
        /// 메인창에서 태그 눌러서 search.
        /// </summary>
        /// <param name="input"></param>
        public void TagSearch(string input)
        {
            OnChange_OpenChatroomSearchMode();
            searchInput.value = input;
            SearchOpenChat(input, true);
            // [gh050404] 태그로 눌르면 서치 없도록 요청됨.
            needSearchSelected = false;
        }

        /// <summary>
        /// 검색창에서 이전 검색어 눌러서 search.
        /// </summary>
        /// <param name="input"></param>
        public void WordSearch(string input)
        {
            searchInput.value = input;
            SearchOpenChat(input, true);
        }

        /// <summary>
        /// 검색하기
        /// </summary>
        public void SearchOpenChat(string input, bool init)
        {
            emptySearchResultObject.SetActive(false);

            if (string.IsNullOrEmpty(input))
            {
            }
            else
            {
                UIUtil.OpenStopWnd(true);

                if (init)
                {
                    // [gh061101] 로그 기록.
                    PanalyzerUtil.PAN_menuItem("DB", "S", input);

                    RoomManager.OpenChatPageNum = 0;
                    AddBeforeOpenChatRoomSearchWord(input);
                    // [gh040906] 초기화한다 .
                    RoomManager.instance.AddAllSearchRoomInfos(null);
                }

                bool flag = false;

                StartCoroutine(OnSend_SearchRoomNameOpenChatRoomList(input, (bool bSuccess) =>
                {
                    flag = bSuccess;

                    if (flag)
                    {
                        if (init)
                        {
                            searchResultScrollEX.Init();
                            searchFirstWrite = false;
                            beforeSearchScrollEX.gameObject.SetActive(false);
                            beforeSeacrchGribEX.gameObject.SetActive(false);
                            beforeSearchRemoveButton.SetActive(false);
                            searchResultScrollEX.gameObject.SetActive(true);
                            searchResultGribEX.gameObject.SetActive(false);
                            filiterResultScrollEX.gameObject.SetActive(false);
                            filiterResultGribEX.gameObject.SetActive(false);

                            var searchRoomList = RoomManager.OpenChatSearchRoomDic.Values.ToList();
                            if (0 != searchRoomList.Count)
                            {
                                searchFilterOption.SetActive(true);
                                searchResultGribEX.gameObject.SetActive(true);
                                searchFristFilterOption[0].SetActive(true);
                                searchSecondFilterOption[0].GetComponent<UIPopupList>().value = "전체";
                                InitSearchSecondFiliterOption();
                                // [gh051407] 활성화 처리 안하면 지워진다.
                                searchSecondFilterOption[0].SetActive(true);
                                searchResultGribEX.Refresh(searchRoomList.Count, true, searchRoomList);
                                //searchResultScrollEX.Refresh();
                                ///위치가 안 맞을 수 있어 한 프레임뒤 정렬(바로 하면 시점이 안 맞을 수 있어서 한 프레임 뒤에 한다.)
                                StartCoroutine(_wait(() =>
                                {
                                    searchResultScrollEX.Init();
                                }));
                            }
                            else
                            {
                                emptySearchResultObject.SetActive(true);
                                UIUtil.OpenWnd(eWindow.POP_OpenChat_Waiting);
                            }
                        }
                        else
                        {
                            var searchRoomList = RoomManager.OpenChatSearchRoomDic.Values.ToList();

                            searchFilterOption.SetActive(true);
                            searchResultGribEX.gameObject.SetActive(true);
                            searchFristFilterOption[0].SetActive(true);
                            searchFristFilterOption[0].GetComponent<UIPopupList>().value = "모든플래닛";
                            searchResultGribEX.Refresh(searchRoomList.Count, false, searchRoomList);
                            //searchResultScrollEX.Refresh();
                            ///위치가 안 맞을 수 있어 한 프레임뒤 정렬(바로 하면 시점이 안 맞을 수 있어서 한 프레임 뒤에 한다.)
                            StartCoroutine(_wait(() =>
                            {
                                searchResultScrollEX.Init();
                            }));
                        }
                    }

                    UIUtil.CloseStopWnd();
                }));
            }
        }

        /// <summary>
        /// 검색어를 변경할때.
        /// </summary>
        /// <param name="input"></param>
        public void OnChange_SearchInput(string input)
        {
            if (!searchFirstWrite)
            {
                if (matchingActive)
                {
                    StartCoroutine(OnSend_MatchingToggleDective((bool bSuccess) =>
                    {
                        if (bSuccess)
                        {
                            matchingButton.value = false;
                            matchingActive = false;
                            matchingOutline.SetActive(matchingActive);
                        }
                    }));
                    return;
                }
            }

            if (!searchFirstWrite && string.IsNullOrEmpty(input))
            {
                OpenChatSearchRoom();
                searchFirstWrite = true;
            }
        }

        /// <summary>
        /// 매칭대기 Active.
        /// </summary>
        /// <param name="uIInput"></param>
        public void InputSendMattingToggle(string uIInput)
        {
            if (string.IsNullOrEmpty(uIInput))
                return;

            StartCoroutine(OnSend_MatchingToggleActive(uIInput, (bool bSuccess) =>
            {
                if (bSuccess)
                {
                    searchInput.value = uIInput;
                    matchingButton.value = true;
                    matchingActive = true;

                    // [gh041612] 해제시 서치 이동 방지. 
                    if (openChatMode == OpenChatMode.Basic)
                        searchFirstWrite = true;
                    else
                        searchFirstWrite = false;

                    // [gh050710] 매칭 토글시 글자 처리.
                    searchText.text = searchInput.value;
                    searchBg.SetActive(true);
                    matchingOutline.SetActive(matchingActive);
                }
            }));
        }

        /// <summary>
        /// 방 Search모드 변경
        /// </summary>
        /// <param name="input"></param>

        public bool needSearchSelected = false;
        public void OnChange_OpenChatroomSearchMode()
        {
            OpenChatSearchRoom();
            needSearchSelected = true;
        }

        private void Update()
        {
            if (needSearchSelected)
            {
                searchInput.isSelected = true;
                needSearchSelected = false;
            }
        }

        /// <summary>
        /// 방 기본모드 변경
        /// </summary>
        /// <param name="input"></param>
        /// [gh050711] 기본 모드에서 서치 정보를 보여줘야 한다.
        public void OnChange_OpenChatroomBasicMode(bool isClearInput = true)
        {
            // [gh050711] 서치 성공시에는 정보 지운다.
            if(isClearInput)
            {
                searchInput.value = "";
            }

            OpenChatBasicRoom();
        }

        public void OnClick_OpenChatroomBasicMode()
        {
            if (!matchingActive && searchInput.value.Length > 0)
            {
                searchInput.value = "";
            }
            // [gh051010] 1105관련.
            isSkipReqList = true;
            OpenChatBasicRoom();
        }

        /// <summary>
        /// 검색 단어 저장.
        /// </summary>
        /// <param name="searchWord"></param>
        public void AddBeforeOpenChatRoomSearchWord(string searchWord)
        {
            BeforeSearchInputString beforWord = MyInfo.instance.UserData.beforeSearchInputStrings.Find(item => item.msearchString == searchWord);
            if (null != beforWord)
            {
                MyInfo.instance.UserData.beforeSearchInputStrings.Remove(beforWord);
            }

            BeforeSearchInputString search = new BeforeSearchInputString(searchWord, DateTime.Now.ToString("MM-dd"));

            MyInfo.instance.UserData.beforeSearchInputStrings.Add(search);

            ///먼저 10개 개수를 체크하지 않고 더한 다음에 개수를 체크해서 10개가 넘을경우 첫번째를 지운다.
            if (11 == MyInfo.instance.UserData.beforeSearchInputStrings.Count)
                MyInfo.instance.UserData.beforeSearchInputStrings.RemoveAt(0);

            DBUtil.MyInfo_SaveData(MyInfo.instance);
        }

        /// <summary>
        /// 기존 검색어를 모두 지운다.
        /// 전체 삭제 버튼
        /// </summary>
        public void AllRemoveBeforeOpenChatRoomSearchWord()
        {
            MyInfo.instance.UserData.beforeSearchInputStrings.Clear();
            beforeSeacrchGribEX.gameObject.SetActive(false);
            beforeSearchRemoveButton.SetActive(false);

            DBUtil.MyInfo_SaveData(MyInfo.instance);
        }

        /// <summary>
        /// 기존 검색어를 지운다.
        /// 하나씩 지우는 x버튼.
        /// </summary>
        /// <param name="removeWord"></param>
        public void RemoveBeforeOpenChatRoomSearchWord(string removeWord)
        {
            BeforeSearchInputString beforWord = MyInfo.instance.UserData.beforeSearchInputStrings.Find(item => item.msearchString == removeWord);
            if (null != beforWord)
            {
                MyInfo.instance.UserData.beforeSearchInputStrings.Remove(beforWord);

                beforeSearchScrollEX.Init();

                if (0 == MyInfo.instance.UserData.beforeSearchInputStrings.Count)
                {
                    beforeSeacrchGribEX.gameObject.SetActive(false);
                    beforeSearchRemoveButton.SetActive(false);
                }
                else
                {
                    beforeSeacrchGribEX.Refresh(MyInfo.instance.UserData.beforeSearchInputStrings.Count, false, MyInfo.instance.UserData.beforeSearchInputStrings);
                    beforeSearchScrollEX.Refresh();
                    ///위치가 안 맞을 수 있어 한 프레임뒤 정렬(바로 하면 시점이 안 맞을 수 있어서 한 프레임 뒤에 한다.)
                    StartCoroutine(_wait(() =>
                    {
                        beforeSearchScrollEX.Init();
                    }));
                }

                DBUtil.MyInfo_SaveData(MyInfo.instance);
            }
        }

        /// <summary>
        /// 검색조건 선택 팝업창 선택.
        /// </summary>
        /// <param name="val"></param>
        public void OnClick_SearchFiliterOption(UIPopupList val)
        {
            searchResultScrollEX.Init();

            if (val.selectIndex == 0)        //  인원수 편집
            {
                searchSecondFilterOption[0].SetActive(true);
                searchSecondFilterOption[0].GetComponent<UIPopupList>().value = "전체";

                StartCoroutine(_wait(() =>
                {
                    var searchRoomList = RoomManager.OpenChatSearchRoomDic.Values.ToList();
                    searchResultGribEX.Refresh(searchRoomList.Count, true, searchRoomList);
                    searchResultScrollEX.Refresh();
                }));
            }
            else if (val.selectIndex == 1)
            {
                InitSearchSecondFiliterOption();
            }
        }

        /// <summary>
        /// 사람수 조건 팝업 선택.
        /// </summary>
        /// <param name="val"></param>
        public void OnClick_SearchFiliterPeopleNUmOption(UIPopupList val)
        {
            searchResultScrollEX.Init();

            if (val.selectIndex == 0)
            {
                StartCoroutine(_wait(() =>
                {
                    var searchRoomList = RoomManager.OpenChatSearchRoomDic.Values.ToList().Where(x => x.OpenChatMaxCount == 2).ToList();
                    searchResultGribEX.Refresh(searchRoomList.Count, true, searchRoomList);
                    searchResultScrollEX.Refresh();
                }));
            }
            else if (val.selectIndex == 1)
            {
                var searchRoomList = RoomManager.OpenChatSearchRoomDic.Values.ToList().Where(x => x.OpenChatMaxCount == 3).ToList();
                searchResultGribEX.Refresh(searchRoomList.Count, true, searchRoomList);
                StartCoroutine(_wait(() =>
                {
                    searchResultScrollEX.Refresh();
                }));
            }
            else if (val.selectIndex == 2)
            {
                StartCoroutine(_wait(() =>
                {
                    var searchRoomList = RoomManager.OpenChatSearchRoomDic.Values.ToList().Where(x => x.OpenChatMaxCount == 4).ToList();
                    searchResultGribEX.Refresh(searchRoomList.Count, true, searchRoomList);
                    searchResultScrollEX.Refresh();
                }));
            }
            else if (val.selectIndex == 3)
            {
                StartCoroutine(_wait(() =>
                {
                    var searchRoomList = RoomManager.OpenChatSearchRoomDic.Values.ToList().Where(x => x.OpenChatMaxCount == 5).ToList();
                    searchResultGribEX.Refresh(searchRoomList.Count, true, searchRoomList);
                    searchResultScrollEX.Refresh();
                }));
            }
            else if (val.selectIndex == 4)
            {
                StartCoroutine(_wait(() =>
                {
                    var searchRoomList = RoomManager.OpenChatSearchRoomDic.Values.ToList().Where(x => x.OpenChatMaxCount == 6).ToList();
                    searchResultGribEX.Refresh(searchRoomList.Count, true, searchRoomList);
                    searchResultScrollEX.Refresh();
                }));
            }
            else if (val.selectIndex == 5)
            {
                StartCoroutine(_wait(() =>
                {
                    var searchRoomList = RoomManager.OpenChatSearchRoomDic.Values.ToList().Where(x => x.OpenChatMaxCount == 7).ToList();
                    searchResultGribEX.Refresh(searchRoomList.Count, true, searchRoomList);
                    searchResultScrollEX.Refresh();
                }));
            }
            else if (val.selectIndex == 6)
            {
                StartCoroutine(_wait(() =>
                {
                    var searchRoomList = RoomManager.OpenChatSearchRoomDic.Values.ToList().Where(x => x.OpenChatMaxCount == 8).ToList();
                    searchResultGribEX.Refresh(searchRoomList.Count, true, searchRoomList);
                    searchResultScrollEX.Refresh();
                }));
            }
            else if (val.selectIndex == 7)
            {
                StartCoroutine(_wait(() =>
                {
                    var searchRoomList = RoomManager.OpenChatSearchRoomDic.Values.ToList();
                    searchResultGribEX.Refresh(searchRoomList.Count, true, searchRoomList);
                    searchResultScrollEX.Refresh();
                }));
            }
        }

        #endregion

        #region Web

        /// <summary>
        /// 오픈플래닛방 입장.
        /// </summary>
        /// <param name="threadkey"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public IEnumerator OnSend_EnterOpenChatRoom(string threadkey, Action<bool, string, string> onComplete)
        {
            bool Success = false;
            string HandleID = string.Empty;
            string Scerct = string.Empty;

            Debug.Log("GH:OnSend_EnterOpenChatRoom " + threadkey + " " + WebProtocols.instance);

            // [gh050904] 서치후 방 입장시 처리 추가. 방 페이지를 -1 해야 한다. 그래야 기존 페이지 유지.
            if (openChatMode == OpenChatMode.Search)
            {
                Debug.Log("GH:OnSend_EnterOpenChatRoom with search " + RoomManager.OpenChatPageNum);
                if(RoomManager.OpenChatPageNum>0)
                    RoomManager.OpenChatPageNum--;
            }

            yield return WebProcessor.instance.StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_OpenChatRoom_Visit(threadkey, (bool bSuccess, string handleId, string secret) =>
            {
                Success = bSuccess;
                if (Success)
                {
                    HandleID = handleId;
                    Scerct = secret;
                }
                else
                {
                    // [gh041310] 성공하든 못하든 기다림 화면 삭제. -> 실패시 삭제 성공하면 VideoCallWnd Start시 삭제.
                    if (isWaitRoomJoin())
                    {
                        SetWaitRoomJoin(false);
                    }
                }
            }));

            onComplete(Success, HandleID, Scerct);
        }

        /// <summary>
        /// 오픈플래닛방 퇴장.
        /// </summary>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public IEnumerator OnSend_OpenChatRoomExit(string threadKey, Action<bool> onComplete)
        {
            bool Success = false;

            yield return StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_OpenChatRoom_Exit(threadKey, (bool bSuccess) =>
            {
                Success = bSuccess;

                if (Success)
                {
                    //통화종료 버튼에 함수 연결시킬떄 성공시 아래 함수 실행으로 변경.
                    //UIUtil.GetWnd<VideoCallWnd>().OpenChatEndAndDestroy();
                }

            }));

            onComplete(Success);
        }

        /// <summary>
        /// 방장의 유저 강퇴 기능.
        /// </summary>
        /// <param name="userNum"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public IEnumerator OnSend_OpenChatRoomKick(long usn, Action<bool> onComplete)
        {
            bool Success = false;

            yield return StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_OpenChatRoom_Kick(usn, (bool bSuccess) =>
            {
                Success = bSuccess;

                if (Success)
                {
                    var result = WebProcessor.instance.RecvInfo.result;
                    string resultMsg = result.GetValue("result_Mag");
                    UIUtil.ShowMessageBox(resultMsg);
                }
            }));

            onComplete(Success);
        }

        /// <summary>
        /// 현재 방의 멤버 조회
        /// </summary>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public IEnumerator OnSend_OpenChatRoomMemberList(Action<bool> onComplete)
        {
            bool Success = false;
            RoomInfo ri = null;

            yield return StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_OpenChatRoom_Members(RoomManager.GetCurrThreadKey(), (bool bSuccess) =>
            {
                Success = bSuccess;

                if (Success)
                {
                    ri = RoomManager.CurrOpenChatroomInfo;
                    ri.OpenChatMemberSetData(WebProcessor.instance.RecvInfo.result);
                }
            }));

            if (!Success)
            {
                UIUtil.CloseStopWnd();
                UIUtil.ShowMessageBox("플래닛 멤버 정보 갱신에 실패하였습니다. 다시 한번 시도해 주세요.");
                yield break;
            }

            List<RoomInfo> roominfo = new List<RoomInfo>();
            roominfo.Add(ri);
            yield return StartCoroutine(WebProtocols.instance.Download_profileImage(roominfo, (bool bSuccess) =>
            {
                Success = bSuccess;

                if (!bSuccess)
                {
                    UIUtil.CloseStopWnd();
                    UIUtil.ShowMessageBox("이미지 다운이 실패하였습니다.");
                }
            }));

            onComplete(Success);
        }

        /// <summary>
        /// OpenChatRoom리스트 조회.(server)
        /// 전부 지우고 조회(추천방, 최신방 함께 조회한다.)
        /// </summary>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        private IEnumerator OnSend_EmptyAndAddOpenChatRoomList(Action<bool> onComplete)
        {
            bool Success = false;

            yield return StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_CHATROOM_Recommand_LIST_REQ((bool bSuccess) =>
            {
                if (bSuccess)
                {
                    Success = bSuccess;
                }
                else
                {
                    //실패시에는 코루틴에 UIUtil.CloseStopWnd();가 들어가 있어서 튕기게 한다.
                    //오류 메시도 코루틴 내의 함수에서 띄운다.
                    onComplete(Success);
                    return;
                }
            }));

            yield return StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_CHATROOM_Recently_LIST_REQ((bool bSuccess) =>
            {
                if (bSuccess)
                {
                    Success = bSuccess;
                }
                else
                {
                    //실패시에는 코루틴에 UIUtil.CloseStopWnd();가 들어가 있어서 튕기게 한다.
                    //오류 메시도 코루틴 내의 함수에서 띄운다.

                    return;
                }
            }));

            UIUtil.CloseStopWnd();

            onComplete(Success);
        }

        /// <summary>
        /// OpenChatRoom리스트 조회.(server)
        /// 추가로 갱신
        /// </summary>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public IEnumerator OnSend_AddOpenChatRoomList(Action<bool> onComplete)
        {
            bool Success = false;

            yield return StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_CHATROOM_Recently_LIST_REQ((bool bSuccess) =>
            {
                Success = bSuccess;

                if (bSuccess)
                {
                    var recentlyRoomList = RoomManager.OpenChatRoomDic.Values.ToList().Where(x => !x.OpenChatrecommand).ToList();

                    if (recentlyRoomList.Count == 0)
                    {
                        recentBG.SetActive(false);
                        recentlyOpenRoomScrollGribEX.Init(recentlyRoomList.Count, recentlyRoomList);
                    }
                    else
                    {
                        recentBG.SetActive(true);
                        recentlyOpenRoomScrollGribEX.Refresh(recentlyRoomList.Count, false, recentlyRoomList);
                    }

                    openChatRoomScrollEX.KSM_Refresh();
                }
                else
                {
                    //실패시에는 코루틴에 UIUtil.CloseStopWnd();가 들어가 있어서 튕기게 한다.
                    //오류 메시도 코루틴 내의 함수에서 띄운다.
                    return;
                }
            }));

            onComplete(Success);
        }

        /// <summary>
        /// OpenChatTag리스트 조회.(server)
        /// </summary>
        /// <returns></returns>
        private IEnumerator OnSend_OpenChatTagList(Action<bool> onComplete)
        {
            bool Success = false;

            yield return StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_Tag_LIST_REQ((bool bSuccess) =>
            {
                if (bSuccess)
                {
                    Success = bSuccess;
                }
                else
                {
                    //실패시에는 코루틴에 UIUtil.CloseStopWnd();가 들어가 있어서 튕기게 한다.
                    //오류 메시도 코루틴 내의 함수에서 띄운다.
                    return;
                }
            }));

            //yield return null;

            //onComplete(true);
            onComplete(Success);
        }

        /// <summary>
        /// OpenChatRoom리스트와 Tag리스트를 동시에 가져온다.(server)
        /// </summary>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public IEnumerator OnSend_OpenChatRoomListandTagList(Action<bool> onComplete)
        {
            bool flag = false;

            yield return StartCoroutine(OnSend_EmptyAndAddOpenChatRoomList((bool bSuccess) =>
            {
                flag = bSuccess;
                if (RoomManager.OpenChatRoomDic.Values.ToList().Count != 0)
                {
                    if (flag)
                    {
                        var recommandRoomList = RoomManager.OpenChatRoomDic.Values.ToList().Where(x => x.OpenChatrecommand).ToList();
                        // [gh042506] 리스트가 3미만 경우 처리.
                        int rCount = recommandRoomList.Count;
                        if (recommandRoomList.Count == 0)
                        {
                            recommandBG.SetActive(false);
                            recommandlOpenRoomScrollGribEX.Refresh(recommandRoomList.Count, true, recommandRoomList);
                        }
                        else
                        {
                            recommandBG.SetActive(true);
                            recommandlOpenRoomScrollGribEX.Refresh(recommandRoomList.Count, true, recommandRoomList);
                        }

                        var recentlyRoomList = RoomManager.OpenChatRoomDic.Values.ToList().Where(x => !x.OpenChatrecommand).ToList();

                        if (recentlyRoomList.Count == 0)
                        {
                            if(rCount == 1)
                            {
                                dragBlanks[0].SetActive(true);
                                dragBlanks[1].SetActive(true);
                            }
                            else if(rCount == 2)
                            {
                                dragBlanks[0].SetActive(true);
                                dragBlanks[1].SetActive(false);
                            }
                            else
                            {
                                dragBlanks[0].SetActive(false);
                                dragBlanks[1].SetActive(false);
                            }
                            recentBG.SetActive(false);
                            recentlyOpenRoomScrollGribEX.Refresh(recentlyRoomList.Count, true, recentlyRoomList);
                        }
                        else
                        {
                            dragBlanks[0].SetActive(false);
                            dragBlanks[1].SetActive(false);
                            recentBG.SetActive(true);
                            recentlyOpenRoomScrollGribEX.Refresh(recentlyRoomList.Count, true, recentlyRoomList);
                        }

                        dragObject.SetActive(false);

                        openChatRoomScrollEX.Refresh();
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    // [gh041105] 없어도 초기화 해준다.
                    recommandBG.SetActive(false);
                    recommandlOpenRoomScrollGribEX.Refresh(0, true, null);
                    dragObject.SetActive(true);
                    openChatRoomScrollEX.Refresh();
                    // [gh050302] 이미지 교체 및 좌표 수정.
                    StartCoroutine(_wait(() =>
                    {
                        openChatRoomScrollEX.Refresh();
                    }));
                }
            }));

            yield return StartCoroutine(OnSend_OpenChatTagList((bool bSuccess) =>
            {
                flag = bSuccess;

                if (flag)
                {
                    //tagUITable.columns = RoomManager.OpenChatTagList.Count;
                    // [gh041402] 태그 초기화 한다.
                    for (int i = 0; i < openChatTagInfo.Length; i++)
                    {
                        if (openChatTagInfo != null)
                            Destroy(openChatTagInfo[i]);
                    }
                    // 

                    for (int i = 0; i < RoomManager.OpenChatTagList.Count; ++i)
                    {
                        if (openChatTagInfo[i] != null)
                        {
                            Destroy(openChatTagInfo[i]);
                        }
                        var tagObject = Instantiate(openChatTag);
                        openChatTagInfo[i] = tagObject;
                        tagObject.transform.SetParent(tagUITable.transform);
                        tagObject.transform.localScale = Vector3.one;
                        openChatTagInfo[i].SetActive(false);
                        tagObject.GetComponent<OpenChat_tag>().SetData(RoomManager.OpenChatTagList[i].tagName);

                        openChatTagInfo[i].SetActive(true);
                    }

                    for (int i = RoomManager.OpenChatTagList.Count; i < 10; ++i)
                    {
                        if (openChatTagInfo[i] == null)
                        {
                            break;
                        }
                        Destroy(openChatTagInfo[i]);
                    }
                }
                else
                {
                    return;
                }
            }));

            yield return StartCoroutine(_wait(() =>
            {
                tagUITable.repositionNow = true;
            }));

            onComplete(flag);
        }

        /// <summary>
        /// 방을 검색한다.(server)
        /// </summary>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public IEnumerator OnSend_SearchRoomNameOpenChatRoomList(string input, Action<bool> onComplete)
        {
            bool Success = false;

            yield return WebProtocols.instance.StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_SearchOpenchatRoomName_REQ(input, (bool bSuccess) =>
            {
                Success = bSuccess;

                if (bSuccess)
                {
                    
                }
            }));

            onComplete(Success);
        }

        /// <summary>
        /// 매칭대기 Active(server)
        /// </summary>
        /// <param name="input"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        private IEnumerator OnSend_MatchingToggleActive(string input, Action<bool> onComplete)
        {
            bool Success = false;

            yield return StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_MatchingToggleActive_REQ(input, (bool bSuccess) =>
            {
                if (bSuccess)
                {
                    Success = bSuccess;
                    // [gh050710] 매칭 키워드 배경처리.
                    searchBg.SetActive(true);
                    // [gh061101] 로그 추가.
                    PanalyzerUtil.PAN_menuItem("DA", "O", "Y");
                    PanalyzerUtil.PAN_menuItem("DB", "S", input);
                }
            }));

            onComplete(Success);
        }

        /// <summary>
        /// 매칭대기 Deactive(server)
        /// </summary>
        /// <param name="input"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        /// [gh041911] 매칭 시 정보 취득을 위해서. public
        public IEnumerator OnSend_MatchingToggleDective(Action<bool> onComplete)
        {
            bool Success = false;

            yield return WebProcessor.instance.StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_MatchingToggleDeactive_REQ((bool bSuccess) =>
            {
                if (bSuccess)
                {
                    Success = bSuccess;
                    // [gh050710] 매칭 키워드 배경처리.
                    searchBg.SetActive(false);
                    // [gh061101] 로그 추가.
                    PanalyzerUtil.PAN_menuItem("DA", "O", "N");
                }
            }));

            onComplete(Success);
        }

        #endregion
    }
}
