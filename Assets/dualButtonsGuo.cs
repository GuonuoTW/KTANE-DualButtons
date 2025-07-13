using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;


public class dualButtonsGuo : MonoBehaviour
{
   public KMBombModule Module;
   public KMBombInfo Bomb;
   public KMAudio Audio;
   public KMSelectable[] Buttons;
   public TextMesh[] ColorblindTexts;
   private static readonly Color[] ColorArray = new Color[] { new Color(0.9f, 0.2f, 0.2f), new Color(0.8f, 0.8f, 0.2f), new Color(0.2f, 0.9f, 0.2f), new Color(0.2f, 0.2f, 0.9f) };
   //Red, Yellow, Green, Blue
   public KMColorblindMode Colorblind;
   private int LeftColorIdx;
   private int RightColorIdx;
   private int PressButton;
   public static int ModuleIdCounter = 1;
   int ModuleId;
   private int TimesPresses = 1;
   private int TimesPressed = 0;
   private bool colorblindActive = false;
   private bool ModuleSolved;
   private Coroutine[] buttonAnimCoroutines = new Coroutine[2];

   void Awake()
   {
      ModuleId = ModuleIdCounter++;
      for (int i = 0; i < Buttons.Length; i++)
      {
         int x = i;
         Buttons[x].OnInteract += delegate () { buttonPress(x); return false; };
      }
      colorblindActive = Colorblind.ColorblindModeActive;
      if (!colorblindActive)
      {
         for (int i = 0; i < ColorblindTexts.Length; i++)
         {
            ColorblindTexts[i].color = Color.clear;
         }
      }
   }

   void buttonPress(int idx)
   {
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[idx].transform);
      if (!ModuleSolved)
      {
         if (idx == PressButton) TimesPressed += 1;
         else CheckAnswer();
      }
      //Debug.Log(Buttons[idx].name + " Pressed");
         if (buttonAnimCoroutines[idx] != null)
         {
            StopCoroutine(buttonAnimCoroutines[idx]);
         }
      buttonAnimCoroutines[idx] = StartCoroutine(buttonAnim(idx));
      Buttons[idx].AddInteractionPunch();
   }

   IEnumerator buttonAnim(int idx)
   {
      float timer = 0;
      float duration = 0.075f;
      float initialPos = 0.0173f;
      float depression = 0.0133f;
      while (timer < duration)
      {
         yield return null;
         timer += Time.deltaTime;
         Buttons[idx].transform.localPosition = new Vector3(Buttons[idx].transform.localPosition.x, Mathf.Lerp(initialPos, depression, timer / duration), Buttons[idx].transform.localPosition.z);
      }
      Buttons[idx].transform.localPosition = new Vector3(Buttons[idx].transform.localPosition.x, depression, Buttons[idx].transform.localPosition.z);

      timer = 0;

      while (timer < duration)
      {
         yield return null;
         timer += Time.deltaTime;
         Buttons[idx].transform.localPosition = new Vector3(Buttons[idx].transform.localPosition.x, Mathf.Lerp(depression, initialPos, timer / duration), Buttons[idx].transform.localPosition.z);
      }
      Buttons[idx].transform.localPosition = new Vector3(Buttons[idx].transform.localPosition.x, initialPos, Buttons[idx].transform.localPosition.z);
   }

   void Start()
   {
      LeftColorIdx = UnityEngine.Random.Range(0, 4);
      RightColorIdx = UnityEngine.Random.Range(0, 4);
      //RYGB
      Buttons[0].GetComponent<MeshRenderer>().material.color = ColorArray[LeftColorIdx];
      Buttons[1].GetComponent<MeshRenderer>().material.color = ColorArray[RightColorIdx];

      //Colorblind
      ColorblindTexts[0].text = "RYGB"[LeftColorIdx].ToString();
      ColorblindTexts[1].text = "RYGB"[RightColorIdx].ToString();

      Step1Func(LeftColorIdx, RightColorIdx);
      Step2Func(LeftColorIdx, RightColorIdx, PressButton);
      //Debug.Log("Press Button is " + PressButton + " Press " + TimesPresses + " Times.");
      Debug.LogFormat("[Dual Buttons #{0}] Press Button is {1}, Press {2} Times.", ModuleId, PressButton == 0 ? "Left" : "Right", TimesPresses);


   }

   void Step1Func(int LCidx, int RCidx)
   {
      if (LCidx == 1 && RCidx == 1) PressButton = 0;
      else if (((LCidx == 0) ^ (RCidx == 0)) && Bomb.GetSerialNumberNumbers().Take(2).Sum() > 10) PressButton = LCidx == 0 ? 0 : 1;
      else if ((LCidx == 2 || RCidx == 2) && Bomb.GetSerialNumberNumbers().Last() > 4) PressButton = 1;
      else if (LCidx == 3) PressButton = 0;
      else if (LCidx == 1 || RCidx == 1) PressButton = 1;
      else PressButton = 0;
   }

   void Step2Func(int LCidx, int RCidx, int PBidx)
   {
      if ((PBidx == 0 && RCidx == 1) || (PBidx == 1 && LCidx == 1)) TimesPresses += 1;
      if (Bomb.GetSolvableModuleNames().Any(x => x.ToLowerInvariant().Contains("wires"))) TimesPresses += 2;
      if ((PBidx == 0 && LCidx == 2) || (PBidx == 1 && RCidx == 2)) TimesPresses += 2;
      if (LCidx == 0 || RCidx == 0) TimesPresses += 1;
      if (LCidx == 1) TimesPresses += 1;
      if (RCidx == 3) TimesPresses += 1;
   }

   void CheckAnswer()
   {
      if (TimesPressed == TimesPresses)
      {
         Module.HandlePass();
         Audio.PlaySoundAtTransform("snd_disarm", this.transform);
         Debug.LogFormat("[Dual Buttons #{0}] Module Solved!", ModuleId);
         ModuleSolved = true;
      }
      else
      {
         Module.HandleStrike();
         Debug.LogFormat("[Dual Buttons #{0}] Press {1} Times, Expecting {2}. Strike!", ModuleId, TimesPressed, TimesPresses);
         TimesPressed = 0;
      }
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} l/r (ex. lllr) to press left/right buttons. | !{0} colo(u)rblind to toggle colorblind.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand(string command)
   {
      command = command.ToLowerInvariant();

      if (command == "colorblind" || command == "colourblind")
      {
         yield return null;
         colorblindActive = !colorblindActive;
         if (!colorblindActive)
         {
            for (int i = 0; i < ColorblindTexts.Length; i++)
            {
               ColorblindTexts[i].color = Color.clear;
            }
         }
         else
         {
            for (int i = 0; i < ColorblindTexts.Length; i++)
            {
               ColorblindTexts[i].color = Color.black;
            }
         }
         yield break;
      }

      for (int i = 0; i < command.Length; i++)
      {
         if (command[i] != 'r' && command[i] != 'l')
         {
            yield return "sendtochaterror Invalid command.";
            yield break;
         }
      }

      yield return null;

      for (int i = 0; i < command.Length; i++)
      {
         if (command[i] == 'l')
         {
            Buttons[0].OnInteract();
         }
         else // r
         {
            Buttons[1].OnInteract();
         }
         yield return new WaitForSeconds(0.2f);
      }
   }

   IEnumerator TwitchHandleForcedSolve()
   {
      if (TimesPressed > TimesPresses)
      {
         TimesPressed = TimesPresses;
      }
      for (int i = TimesPressed; i < TimesPresses; i++)
      {
         Buttons[PressButton].OnInteract();
         yield return null;
      }
      Buttons[1 - PressButton].OnInteract();
   }
}
