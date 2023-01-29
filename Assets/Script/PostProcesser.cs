using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcesser : MonoBehaviour
{
    public Volume pope;
    public ChromaticAberration chromatic;
    
    public float inChromaticTime;
    public float chromaticRunTime;
    public float outChromaticTime;
    public float starChromaticAmount;
    
    private Coroutine _coroutine;
    private IEnumerator a; 
    private void Start()
    {
        TryGetComponent<Volume>(out pope);
        pope.profile.TryGet(out chromatic);
    }
    
    private void Update()
    {
        // 이벤트로 받아올 것
        if (Input.GetKeyDown(KeyCode.P))
        {
            _coroutine = StartCoroutine(IntroChromatic());
            a = IntroChromatic();
            StartCoroutine(a);
        }
        
        // if (Input.GetKeyDown(KeyCode.E))
        // {
        //     _coroutine = StartCoroutine(InChromatic());
        // }
        
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _coroutine = StartCoroutine(OutChromatic());
        }
        
        if(Input.GetKeyDown(KeyCode.X))
            StopCoroutine(_coroutine);
    }

    IEnumerator IntroChromatic()
    {
        chromatic.active = true; 
        while (chromatic.intensity.value < 1)
        {
            chromatic.intensity.value += inChromaticTime * Time.deltaTime;
            yield return null;
        }
        chromatic.intensity.value = 1;
    }

    IEnumerator InChromatic()
    {
        chromatic.active = true;
        chromatic.intensity.value = 1;
        yield return new WaitForSeconds(chromaticRunTime);
    }
            
    IEnumerator OutChromatic()
    {
        chromatic.active = true;
        chromatic.intensity.value = starChromaticAmount;
        while (chromatic.intensity.value > 0)
        {
            chromatic.intensity.value -= outChromaticTime * Time.deltaTime;

            yield return null;
        }

        chromatic.active = false;
        chromatic.intensity.value = 0;
    }
}