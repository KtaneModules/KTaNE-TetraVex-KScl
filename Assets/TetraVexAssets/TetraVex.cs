using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

using RNG = UnityEngine.Random;

public class TetraVex : MonoBehaviour
{
	// Standardized logging
	private static int globalLogID = 0;
	private int thisLogID;
	private bool moduleSolved;

	/*********************************************************************
		To Timwi, and/or anyone else writing Souvenir support for this
		or any of my other modules:  This is a request that you do not
		modify the functionality of my module or the appearance of any
		labels from within Souvenir itself.  I do not like the idea of
		another module changing the functionality of my own to fit its
		whims, and feel it results in some unwelcome surprises for the
		experts and defusers alike; especially newer ones or those who
		are still trying to learn the module.
	*********************************************************************/

	public KMBombInfo bombInfo;
	public KMAudio bombAudio;
	public KMBombModule bombModule;

	public Material[] colorMeshes;

	public KMSelectable checkButton;

	public VexBlock[] vexes;
	public KMSelectable[] vexButtonTargets;

	private VexBlock currentlyHeldVex = null;
	private int openPosition = -1;

	private readonly string[] __positionText = new string[] {
		   "top-left",    "top-middle",    "top-right",
		"middle-left", "middle-middle", "middle-right",
		"bottom-left", "bottom-middle", "bottom-right"
	};


	// -----
	// Helper functions
	// -----

	// Gets all Vex Blocks in the order they're displayed on screen.
	// Used for the solve checker.
	List<VexBlock> VexesInDisplayOrder()
	{
		List<VexBlock> ret = vexes.ToList();
		ret.Sort(delegate(VexBlock x, VexBlock y) {
			return x.GetPosition() - y.GetPosition();
		});
		return ret;
	}

	// Gets Vex Block at a position, if any. Used for input handler, mostly.
	VexBlock VexAtPosition(int p)
	{
		for (int i = 0; i < 9; ++i)
		{
			if (vexes[i].GetPosition() == p)
				return vexes[i];
		}
		return null;
	}


	// -----
	// Puzzle generation
	// -----

	void GeneratePuzzle()
	{
		int digit;
		for (int x = 0; x < 3; ++x)
		{
			digit = RNG.Range(0, 8);
			vexes[x    ].SetFace(2, digit, colorMeshes[digit]);
			digit = RNG.Range(0, 8);
			vexes[x    ].SetFace(0, digit, colorMeshes[digit]);
			vexes[x + 3].SetFace(2, digit, colorMeshes[digit]);
			digit = RNG.Range(0, 8);
			vexes[x + 3].SetFace(0, digit, colorMeshes[digit]);
			vexes[x + 6].SetFace(2, digit, colorMeshes[digit]);
			digit = RNG.Range(0, 8);
			vexes[x + 6].SetFace(0, digit, colorMeshes[digit]);
		}
		for (int y = 0; y < 9; y += 3)
		{
			digit = RNG.Range(0, 8);
			vexes[y    ].SetFace(1, digit, colorMeshes[digit]);
			digit = RNG.Range(0, 8);
			vexes[y    ].SetFace(3, digit, colorMeshes[digit]);
			vexes[y + 1].SetFace(1, digit, colorMeshes[digit]);
			digit = RNG.Range(0, 8);
			vexes[y + 1].SetFace(3, digit, colorMeshes[digit]);
			vexes[y + 2].SetFace(1, digit, colorMeshes[digit]);
			digit = RNG.Range(0, 8);
			vexes[y + 2].SetFace(3, digit, colorMeshes[digit]);
		}

		// Scramble the pieces now that we've generated the solution.
		List<int> vexPositions = new List<int> {0, 1, 2, 3, 4, 5, 6, 7, 8};
		vexPositions.Shuffle();
		for (int i = 0; i < 9; ++i)
		{
			vexes[i].idealPosition = i;
			vexes[i].SetPosition(vexPositions[i]);
		}

		// Logging
		Debug.LogFormat("[TetraVex #{0}] Intended solution:", thisLogID);
		for (int i = 0; i < 9; i += 3)
		{
			string[] log = VexBlock.BlockLineToStrings(vexes[i], vexes[i+1], vexes[i+2]);
			Debug.LogFormat("[TetraVex #{0}] {1}", thisLogID, log[0]);
			Debug.LogFormat("[TetraVex #{0}] {1}", thisLogID, log[1]);
			Debug.LogFormat("[TetraVex #{0}] {1}", thisLogID, log[2]);
			if (i < 6)
				Debug.LogFormat("[TetraVex #{0}] ───┼───┼───", thisLogID);
		}
	}


	// -----
	// Interactions, etc
	// -----

	bool VexTargetInteract(int pressed)
	{
		if (moduleSolved)
			return false;

		VexBlock target = VexAtPosition(pressed);

		// Pressed the empty square?
		if (target == null)
		{
			bombAudio.PlaySoundAtTransform("ClickDown", vexButtonTargets[pressed].transform);
			currentlyHeldVex.SetPosition(openPosition);
			currentlyHeldVex = null;
			openPosition = -1;
			return false;
		}

		// Already holding something? Set it down before picking up.
		if (currentlyHeldVex != null)
		{
			currentlyHeldVex.SetPosition(pressed);
			bombAudio.PlaySoundAtTransform("ClickDown", vexButtonTargets[pressed].transform);
		}
		else
			openPosition = target.GetPosition();
		target.PickUp();
		currentlyHeldVex = target;
		return false;
	}

	bool CheckButtonPress()
	{
		bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, checkButton.transform);
		checkButton.AddInteractionPunch(0.4f);
		checkButton.GetComponent<Animator>().Play("ZButtonPress", 0, 0);

		if (moduleSolved)
			return false;

		List<VexBlock> shownOrder = VexesInDisplayOrder();
		if (shownOrder[0].GetPosition() == -1)
		{
			Debug.LogFormat("[TetraVex #{0}] STRIKE: Submitted when a piece wasn't in place.", thisLogID);
			bombModule.HandleStrike();
			return false;
		}

		// Logging
		Debug.LogFormat("[TetraVex #{0}] Submitted solution:", thisLogID);
		for (int i = 0; i < 9; i += 3)
		{
			string[] log = VexBlock.BlockLineToStrings(shownOrder[i], shownOrder[i+1], shownOrder[i+2]);
			Debug.LogFormat("[TetraVex #{0}] {1}", thisLogID, log[0]);
			Debug.LogFormat("[TetraVex #{0}] {1}", thisLogID, log[1]);
			Debug.LogFormat("[TetraVex #{0}] {1}", thisLogID, log[2]);
			if (i < 6)
				Debug.LogFormat("[TetraVex #{0}] ───┼───┼───", thisLogID);
		}

		// We only check DOWN and RIGHT for each block, because left and up for each block are already checked by previous blocks.
		// We don't even bother extending the loop to hit the bottom-right block, because if we did hit it it is a guaranteed solve.
		for (int i = 0; i < 8; ++i)
		{
			if (!shownOrder[i].IsPositionOkay(i < 6 ? shownOrder[i + 3] : null, i % 3 < 2 ? shownOrder[i + 1] : null))
			{
				Debug.LogFormat("[TetraVex #{0}] STRIKE: The block in the {1} position has an incorrect connection.", thisLogID, __positionText[i]);
				bombModule.HandleStrike();
				return false;
			}
		}

		Debug.LogFormat("[TetraVex #{0}] SOLVE: All connections okay; this is a valid solution.", thisLogID);
		bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, bombModule.transform);
		bombModule.HandlePass();
		moduleSolved = true;
		return false;
	}

	void Awake()
	{
		thisLogID = ++globalLogID;

		for (int i = 0; i < vexButtonTargets.Length; ++i)
		{
			int j = i;
			vexButtonTargets[i].OnInteract += delegate() {
				return VexTargetInteract(j);
			};
		}
		checkButton.OnInteract += CheckButtonPress;

		GeneratePuzzle();
	}


	// -----
	// Twitch Plays Support
	// -----

	private KMSelectable GetTargetSelectable(string id)
	{
		int selNum = 0;
		id = id.ToUpper();

		// Coordinates
		if (id[1] >= '1' && id[1] <= '3')
		{
			selNum = (id[0] - 'A');
			selNum += (id[1] - '1') * 3;
		}
		else
		{
			if (id[0] == 'M')
				selNum = 3;
			else if (id[0] == 'B')
				selNum = 6;

			if (id[1] == 'M')
				selNum += 1;
			else if (id[1] == 'R')
				selNum += 2;
		}

		return vexButtonTargets[selNum];
	}

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Swap two pieces with '!{0} swap B2 C3' or '!{0} swap MM BR'. Submit with '!{0} check'. Chain commands with semicolons.";
#pragma warning restore 414

	public IEnumerator ProcessTwitchCommand(string command)
	{
		Match mt;
		List<string> cmds = command.Split(';').ToList();
		bool anyCommandValid = false;

		foreach (string cmd in cmds)
		{
			if (Regex.IsMatch(cmd, @"^\s*(?:press)?\s*(?:check|submit)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
			{
				yield return null;
				yield return new KMSelectable[] { checkButton };

				// There's no point continuing -- either this is a solve or a strike, and either terminates us.
				yield break;
			}
			if ((mt = Regex.Match(cmd, @"^\s*(?:swap\s+)?([A-C][1-3]|[TMB][LMR])\s*([A-C][1-3]|[TMB][LMR])\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
			{
				KMSelectable targetA = GetTargetSelectable(mt.Groups[1].ToString());
				KMSelectable targetB = GetTargetSelectable(mt.Groups[2].ToString());

				yield return null;
				anyCommandValid = true;

				if (targetA == targetB)
					continue; // Yeah sure, we totally swapped the piece with itself!

				yield return new KMSelectable[] { targetA };
				yield return new WaitForSeconds(0.3f);
				yield return new KMSelectable[] { targetB };
				yield return new WaitForSeconds(0.3f);
				yield return new KMSelectable[] { targetA };
				yield return new WaitForSeconds(0.3f);

				continue;
			}

			if (anyCommandValid)
			{
				yield return String.Format("sendtochaterror I'm not sure what you mean by \"{0}\".", cmd);
				yield break;
			}
		}
		yield break;
	}

	public IEnumerator TwitchHandleForcedSolve()
	{
		if (moduleSolved)
			yield break;
		Debug.LogFormat("[TetraVex #{0}] Force solve requested by Twitch Plays.", thisLogID);

		int i, pos;
		while (true)
		{
			pos = 0;
			for (i = 0; i < 9; ++i)
			{
				// If this vex isn't in the location it started in
				if ((pos = vexes[i].GetPosition()) != i)
				{
					// Hold this vex, wherever it is
					vexButtonTargets[pos].OnInteract();
					yield return new WaitForSeconds(0.4f);
					break;
				}
			}
			// All vexes are in their starting locations?
			if (i == 9)
				break;

			// While we're not holding the vex that goes in the empty spot
			while (currentlyHeldVex.idealPosition != pos)
			{
				// Swap it into the position it started in
				vexButtonTargets[currentlyHeldVex.idealPosition].OnInteract();
				yield return new WaitForSeconds(0.4f);
			}
			// Set down the held piece into the position it started in
			vexButtonTargets[pos].OnInteract();
			yield return new WaitForSeconds(0.4f);
		}
		// Now check to solve.
		checkButton.OnInteract();
	}
}
