using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class Reset : MonoBehaviour {

	public GridManager gridManager;

	public void resetButtonMethod() {
		gridManager.Reset();
	}
}
