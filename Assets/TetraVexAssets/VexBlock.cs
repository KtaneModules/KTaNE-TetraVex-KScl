using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;

public class VexBlock : MonoBehaviour
{
	public Animator anim;

	// DLUR order
	public Renderer[] faces;
	public TextMesh[] labels;
	private int[] digits = new int[4];

	public int idealPosition;
	private int position;

	// For logging
	static public string[] BlockLineToStrings(VexBlock a, VexBlock b, VexBlock c)
	{
		return new string[] {
			String.Format(" {0} │ {1} │ {2} ", a.digits[2], b.digits[2], c.digits[2]),
			String.Format("{0} {1}│{2} {3}│{4} {5}", a.digits[1], a.digits[3], b.digits[1], b.digits[3], c.digits[1], c.digits[3]),
			String.Format(" {0} │ {1} │ {2} ", a.digits[0], b.digits[0], c.digits[0]),
		};
	}

	// Used in initial setup only
	public void SetFace(int face, int digit, Material mat)
	{
		digits[face] = digit;
		labels[face].text = digit.ToString();
		faces[face].material = mat;
	}

	public int GetPosition()
	{
		return position;
	}

	public void SetPosition(int newPosition)
	{
		if (position == -1)
			anim.Play(String.Format("HoldTo0{0}", newPosition + 1), 0, 0);
		else
			anim.Play(String.Format("Position0{0}", newPosition + 1), 0, 0);
		position = newPosition;
	}

	public void PickUp()
	{
		if (position != -1)
			anim.Play(String.Format("0{0}ToHold", position + 1), 0, 0);
		else
			anim.Play("PositionHold", 0, 0);
		position = -1;
	}

	public bool IsPositionOkay(VexBlock down, VexBlock right)
	{
		if (down  != null && down.digits[2]  != digits[0])
			return false;
		if (right != null && right.digits[1] != digits[3])
			return false;
		return true;
	}
}
