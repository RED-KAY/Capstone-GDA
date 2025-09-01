using UnityEngine;
using System.Collections;

/**
 *	Rapidly sets a light on/off.
 *	
 *	(c) 2015, Jean Moreno
**/

[RequireComponent(typeof(Light))]
public class WFX_LightFlicker : MonoBehaviour
{
	public float time = 0.05f;
	
	private float timer;
	
	private Light m_Light;
	
	void Start ()
	{
		timer = time;
		m_Light = GetComponent<Light>();
	}

	private void OnEnable()
	{
		StartCoroutine("Flicker");
	}
	
	IEnumerator Flicker()
	{
		while(true)
		{
			do
			{
				timer -= Time.deltaTime;
				yield return null;
			}
			while(timer > 0);
			m_Light.enabled = false;
			timer = time;
		}
	}
}

