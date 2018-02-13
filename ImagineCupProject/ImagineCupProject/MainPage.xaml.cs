﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using Microsoft.CognitiveServices.SpeechRecognition;
using System.Collections;
using Google.Cloud.Language.V1;
using Google.Protobuf.Collections;
using static Google.Cloud.Language.V1.AnnotateTextRequest.Types;
using ImagineCupProject.EmergencyResponseManuals;
using Aylien.TextApi;
using System.Windows.Input;

namespace ImagineCupProject
{
    /// <summary>
    /// Interaction logic for MainPage.xaml
    /// </summary>
    public partial class MainPage : UserControl
    {
        MicrophoneRecognitionClient microphoneRecognitionClient;
        AzureDatabase azureDatabase;
        Duration duration = new Duration(new TimeSpan(0, 0, 0, 0, 500));

        AutoResetEvent finalResponceEvent;
        string time = DateTime.Now.ToString("yyyy-MM-dd  HH:mm");

        SimpleManual simpleManual = new SimpleManual();
        StandardManual standardManual = new StandardManual();
        ClassifiedManual classifiedManual = new ClassifiedManual();
        MedicalManual medicalManual = new MedicalManual();

        Client client = new Client("3b49bfce", "d5788d26c944e091562527416046febb");
        string text = "I am the passenger and I see the Starbucks building at New York subway station is on fire. I think 911 need to check this out quickly" +
                "At least 37 people have been killed and dozens injured in a fire at a hospital and nursing home in New York, in the country's deadliest blaze for a decade";
        string speechRecognitionResult;
        ArrayList textArrayList = new ArrayList();
        ArrayList textShapeArrayList = new ArrayList();
        AdditionalQuestion additionalQuestion;
        TotalPage totalPage = new TotalPage();
        MainQuestion mainQuestion;
        private readonly ToastViewModel toastViewModel;

        EventVO currentEvent = new EventVO();

        public MainPage()
        {
            InitializeComponent();
            DataContext = toastViewModel = new ToastViewModel();
            additionalQuestion = new AdditionalQuestion(toastViewModel, loadingProcess, currentEvent);
            mainQuestion = new MainQuestion(additionalQuestion, toastViewModel, loadingProcess, currentEvent);
            mainFrame.Content = mainQuestion;
           
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        
        private void ButtonOpenMenu_Click(object sender, RoutedEventArgs e)
        {
            ButtonOpenMenu.Visibility = Visibility.Collapsed;
            ButtonCloseMenu.Visibility = Visibility.Visible;
        }

        private void ButtonCloseMenu_Click(object sender, RoutedEventArgs e)
        {
            ButtonOpenMenu.Visibility = Visibility.Visible;
            ButtonCloseMenu.Visibility = Visibility.Collapsed;
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            if (nextButton.Content.Equals("Next"))
            {
                //EventNUMBER는 AUTO INCREMENT로 설정, PRIMARY KEY로 설정
                currentEvent.EventNUMBER = null;
                currentEvent.EventOPERATOR = mainQuestion.operatorText.Text;
                currentEvent.EventSTARTTIME = mainQuestion.timeText.Text;
                currentEvent.EventENDTIME = null;
                currentEvent.EventLOCATION = mainQuestion.locationText.Text;
                currentEvent.EventPHONENUMBER = mainQuestion.phoneNumberText.Text;
                currentEvent.EventCALLERNAME = mainQuestion.callerNameText.Text;
                currentEvent.EventPROBLEM = mainQuestion.problemText.Text;
                currentEvent.EventCODE = mainQuestion.codeText.Text;

                if (currentEvent.EventLOCATION == "")
                {
                    toastViewModel.ShowError("Location data is missing. Ask where is the accident scene.");
                    return;
                }
                else if(currentEvent.EventPROBLEM == "")
                {
                    toastViewModel.ShowError("Problem data is missing. Ask what is the problem.");
                    return;
                }

                additionalQuestion.location.Text = currentEvent.EventLOCATION;

                //MainPage, MainQuestion, AdditionalQuestion CurrentEvent VO 동기화 작업
                mainQuestion.CurrentEventVO = currentEvent;
                additionalQuestion.CurrentEventVO = currentEvent;

                //VO 객체 값 할당된 거 확인하는 용도, 나중에 지울 것
                MessageBox.Show(currentEvent.EventCODE + "\n" + currentEvent.EventOPERATOR + "\n" + currentEvent.EventSTARTTIME + "\n" +
                    currentEvent.EventENDTIME + "\n" + currentEvent.EventLOCATION+ "\n" + currentEvent.EventPHONENUMBER + "\n" +
                    currentEvent.EventCALLERNAME + "\n" + currentEvent.EventPROBLEM + "\n" + currentEvent.EventCODE + "\n" +
                    currentEvent.EventFirstANSWER + "\n" + currentEvent.EventSecondANSWER + "\n" + currentEvent.EventThirdANSWER + "\n" +
                    currentEvent.EventFourthANSWER + "\n" + currentEvent.EventFifthANSWER + "\n" + currentEvent.EventSixthANSWER + "\n" +
                    currentEvent.EventSeventhANSWER + "\n" + currentEvent.EventEighthANSWER);

                mainFrame.Content = additionalQuestion;
                nextButton.Content = "Previous";
            }
            else
            {
                mainQuestion.operatorText.Text = currentEvent.EventOPERATOR;
                mainQuestion.timeText.Text = currentEvent.EventSTARTTIME;
                mainQuestion.locationText.Text = currentEvent.EventLOCATION;
                mainQuestion.phoneNumberText.Text = currentEvent.EventPHONENUMBER;
                mainQuestion.callerNameText.Text = currentEvent.EventCALLERNAME;
                mainQuestion.problemText.Text = currentEvent.EventPROBLEM;

                //MainPage, MainQuestion, AdditionalQuestion CurrentEvent VO 동기화 작업
                currentEvent = additionalQuestion.CurrentEventVO;
                mainQuestion.CurrentEventVO = currentEvent;

                //카테고리가 나오기 전에 다음 화면으로 넘어갔을 경우 현재사건VO 객체에 코드 정보가 저장이 안 되어있기 때문에,
                //다음 화면(AdditionalQuestion 화면)에서 카테고리 결과가 출력되면 VO 객체에 값을 넣어줌
                currentEvent.EventCODE = mainQuestion.classifiedResult;
                mainQuestion.codeText.Text = currentEvent.EventCODE;

                //VO 객체 값 할당된 거 확인하는 용도, 나중에 지울 것
                MessageBox.Show(currentEvent.EventCODE + "\n" + currentEvent.EventOPERATOR + "\n" + currentEvent.EventSTARTTIME + "\n" +
                    currentEvent.EventENDTIME + "\n" + currentEvent.EventLOCATION + "\n" + currentEvent.EventPHONENUMBER + "\n" +
                    currentEvent.EventCALLERNAME + "\n" + currentEvent.EventPROBLEM + "\n" + currentEvent.EventCODE + "\n" +
                    currentEvent.EventFirstANSWER + "\n" + currentEvent.EventSecondANSWER + "\n" + currentEvent.EventThirdANSWER + "\n" +
                    currentEvent.EventFourthANSWER + "\n" + currentEvent.EventFifthANSWER + "\n" + currentEvent.EventSixthANSWER + "\n" +
                    currentEvent.EventSeventhANSWER + "\n" + currentEvent.EventEighthANSWER);

                mainFrame.Content = mainQuestion;
                nextButton.Content = "Next";
            }
        }

        private void listViewItem1_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mainFrame.Content = totalPage;
        }

        private void listViewItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mainFrame.Content = mainQuestion;
        }

        //음성인식버튼
        private void btnStartRecord_Click(object sender, RoutedEventArgs e)
        {
            toastViewModel.ShowInformation("Receiving the call.");
            ConvertSpeechToText();
        }
        

        //Azure SpeechToText
        private void ConvertSpeechToText()
        {
            var speechRecognitionMode = SpeechRecognitionMode.LongDictation;  //LongDictation 대신 ShortPhrase 선택
            string language = "en-us";
            string subscriptionKey = "39f4a264949c435fba61ff86acc47043";
            //string subscriptionKey = ConfigurationManager.AppSettings["5e3c0f17ea3f40b39cfb6ec28c77bf3e"];
            microphoneRecognitionClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                speechRecognitionMode,
                language,
                subscriptionKey
                );

            //_microphoneRecognitionClient.OnResponseReceived += ResponseReceived;
            microphoneRecognitionClient.OnPartialResponseReceived += ResponseReceived;
            //_microphoneRecognitionClient.OnResponseReceived += OnMicShortPhraseResponseReceivedHandler;
            microphoneRecognitionClient.OnResponseReceived += OnMicDictationResponseReceivedHandler;
            microphoneRecognitionClient.StartMicAndRecognition();
        }

        //Textbox에 text입력
        private void ResponseReceived(object sender, PartialSpeechResponseEventArgs e)
        {
            speechRecognitionResult = e.PartialResult;
            //locationText.Text += result;
            Dispatcher.Invoke(() =>
            {
                /*
                if(e.PartialResult.Contains("am"))
                {
                    temp = e.PartialResult;
                    Responsetxt.Text = temp.Replace("am", "is"); ;
                    Responsetxt.Text += ("\n");
                }
                */
                speechRecognition.Text = (e.PartialResult);
                //mainQuestion.
            });
        }

        //LongDictation으로 설정했을때 receiveHandlear (문장 초기화 되기 전)
        private void OnMicDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            //if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation || e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            //{
            Dispatcher.Invoke(
                (Action)(() =>
                {
                    //_microphoneRecognitionClient.EndMicAndRecognition();

                    //mainQuestion.locationText.Text += "HI";
                    WriteResponseResult(e);
                }));
            //}
        }
        //receiveHandlear 내용 출력 메소드
        private void WriteResponseResult(SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.Results.Length == 0)
            {
                //codeText.Text += "No phrase response is available.";
            }
            else
            {
                //codeText.Text += "********* Final n-BEST Results *********";
                for (int i = 0; i < e.PhraseResponse.Results.Length; i++)
                {
                    //아래내용 다른 textbox에 +=하면 된다. 
                    //callerStatement.Text += e.PhraseResponse.Results[i].DisplayText; // e.PhraseResponse.Results[i].Confidence +
                    string text = e.PhraseResponse.Results[i].DisplayText;
                    var client = LanguageServiceClient.Create();
                    var response = client.AnnotateText(new Document()
                    {
                        Content = text,
                        Type = Document.Types.Type.PlainText
                    },
                    new Features() { ExtractSyntax = true });
                    CorrectSentences(response.Sentences, response.Tokens);
                }
                //codeText.Text += "\n";
            }
        }

        //음성 끊길때마다 문장을 . 표시로 구별해주기
        private async void CorrectSentences(IEnumerable<Google.Cloud.Language.V1.Sentence> sentences, RepeatedField<Token> tokens)
        {
            foreach (var token in tokens)
            {
                if (token.PartOfSpeech.Tag.ToString().Equals("Verb"))
                {
                    if (textShapeArrayList[textShapeArrayList.Count - 1].ToString().Equals("Det") | textShapeArrayList[textShapeArrayList.Count - 1].ToString().Equals("Noun") | textShapeArrayList[textShapeArrayList.Count - 1].ToString().Equals("Pron"))
                    {
                        if (!(textArrayList.Count.ToString().Equals("1") | textArrayList.Count.ToString().Equals("2")))
                        {
                            string temp = textArrayList[textArrayList.Count - 2].ToString().Remove(textArrayList[textArrayList.Count - 2].ToString().Length - 1) + ". ";
                            textArrayList.RemoveAt(textArrayList.Count - 2);
                            textArrayList.Insert(textArrayList.Count - 1, temp);
                        }
                    }
                }
                textArrayList.Add(token.Text.Content + " ");
                textShapeArrayList.Add(token.PartOfSpeech.Tag);
            }
            for (int i = 0; i < textArrayList.Count; i++)
            {
                callerStatement.Text += textArrayList[i];
                mainQuestion.problemText.Text += textArrayList[i];
                //mainQuestion.responseText.Text += textArrayList[i];
            }

            //280, 200, 250자 정도에서 주기적으로 분석
            if (callerStatement.Text.Length > 200)
            {
                mainQuestion.textClassify.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                callerStatement.Text = "caller statement: ";
            }

            //mainQuestion.analyze();
            //MessageBox.Show(mainQuestion.responseText.Text); 
            textArrayList.Clear();
        }
        
        private void btnSendTo112_Click(object sender, RoutedEventArgs e)
        {
            //mainQuestion.sendTo112();
            toastViewModel.ShowSuccess("Send To 112 Success");
            //_vm.ShowWarning(String.Format("{0} Warning", _count++));
            //_vm.ShowError(String.Format("{0} Error", _count++));
        }

        private void btnSendTo110_Click(object sender, RoutedEventArgs e)
        {
            //mainQuestion.sendTo110();
            toastViewModel.ShowSuccess("Send To 110 Success");
            //_vm.ShowInformation(String.Format("{0} Information", _count++));
            //_vm.ShowSuccess(String.Format("{0} Success", _count++));
        }

        private void listViewItem2_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mainFrame.Content = new _112DataPage();
        }

        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            //mainQuestion.analyze();
            toastViewModel.ShowSuccess("Dispatch completed");
        }
    }
}
