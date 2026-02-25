using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class Demo : MonoBehaviour
{
	[SerializeField]
	private	Roulette	roulette;
	[SerializeField]
	private	Button		buttonSpin;

	private void Awake()
	{
		buttonSpin.onClick.AddListener(()=>
		{
			buttonSpin.interactable = false;
			roulette.Spin(EndOfSpin).Forget();
		});
	}

	private void EndOfSpin(RoulettePieceData selectedData)
	{
		buttonSpin.interactable = true;

		Debug.Log($"{selectedData.index}:{selectedData.description}");
	}
}

