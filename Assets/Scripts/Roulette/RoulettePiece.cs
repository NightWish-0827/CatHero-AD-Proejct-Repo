using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoulettePiece : MonoBehaviour
{
	[SerializeField]
	private	Image			imageIcon;
	//[SerializeField]
	//private	TextMeshProUGUI	textDescription;

	public void Setup(RoulettePieceData pieceData)
	{
		imageIcon.sprite		= pieceData.icon;
		//textDescription.text	= pieceData.description;
	}
}

