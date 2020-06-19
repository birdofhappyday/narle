using System;
using System.Collections.Generic;
using Assets.Scripts.Utils;
using System.Linq;
using System.Text;
using UnityEngine;
using Assets.Scripts.UI.Windows.Popup;
using Assets.Scripts.User;
using Assets.Scripts.Cores;
using Assets.Scripts.Payment;
using Assets.Scripts.Networks.Web;
// 이 스크립트의 목적은 하트 구매용.
namespace Assets.Scripts.UI.Windows.Lobby.OpenChat
{
    public class OpenChat_ChargeJew : UISwipeWndBase
    {
        //UI적인 기능.
        public UILabel possessCoin; //보유코인
                                    //리스트 정보를 가져올 프리팹이 필요하고.
        public UILabel buyheart;    //구매하게될 하트 정보. = for문 돌면서.

        public override void Open(params object[] args)
        {
            base.Open(args);

            //보여질 때 해당값으로 셋팅하는부분이 필요.
            RecoverWnd();
        }

        //이 상황은 다른 루틴에서 타고 들어올 때 다시 처리 한 부분.
        public override void RecoverWnd()
        {
            possessCoin.text = MyInfo.instance.UserData.coin + "";
            base.RecoverWnd();
        }

        public void Onclick_CoinBy(GameObject obj, GameObject heartValue)
        {
            int price = int.Parse(obj.GetComponent<UILabel>().text);
            int pCoin = int.Parse(possessCoin.text);
            int heart = int.Parse(heartValue.GetComponent<UILabel>().text);
            ScriptMgr.instance.Get<PurchaseManager>().HeartItemInfoListShow();
            //구매 처리. 소비자 보유 금액과 물품 구매가격이 음수가 아닐 때
            //구매 처리 및 상황에 맞게 코인이 부족할 때에는 리턴처리.
            if (price != -1)
            {
                if (MyInfo.instance.UserData.coin >= price)
                {
                    //구매가 완료 시.
                    //실제 처리는 서버에서 처리. 여기서 서버에게 요청 해서 받고.
                    //지출 코드 삽입
                    //서버에서 받은 결과 값으로 UI라벨 값 변경.
                    //외부에서 받아온 하트 보유량과 충전 완료된 하트 합산값.
                    // [gh0329] 현재는 테스트상 하트 10개짜리만 한다.
                    StartCoroutine(WebProtocols.instance.OnSend_OPENCHAT_HeartBuy(40001, (bool bSuccess) =>
                    {
                        // add [gh0329] 성공시 하트 갱신필요.
                        if (bSuccess)
                        {
                            OpenChatWnd wnd = UIUtil.GetWnd<OpenChatWnd>(false);
                            if (wnd != null)
                            {
                                wnd.OnHeartCount();
                            }
                            possessCoin.text = (pCoin -= price) + "";
                            UIUtil.GetWnd<POP_Confirm_Buying>().Open(this.name, heart, price);
                        }
                        else
                        {
                            UIUtil.ShowMessageBox("하트 구매 실패");
                        }
                    }));

                    //여기서 서버랑 통신.
                    // StartCoroutine().
                    //콜백 처리.
                }
                //구매가 완료가 되지않을 시.
                else
                {
                    UIUtil.GetWnd<POP_Confirm_Shortage>().Open(price);
                }
            }
        }
        public void SettingCoinMoveBtn()
        {
            UIUtil.Swipe(eWindow.Setting_ChargeCoin, eSwipe_Option.NoneClear, this.name);
        }
        public void RecoverWndConnect()
        {
            RecoverWnd();
        }
        public override void Close(params object[] args)
        {
            base.Close(args);
        }

        public override void OnClick_Back()
        {
            UIUtil.SwipeBack();
        }
    }
}
