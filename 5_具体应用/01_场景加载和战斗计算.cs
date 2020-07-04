using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TestExample : MonoBehaviour
{
    private float progress;
    public Text progressText;
    public Slider slider;

    void Start()
    {
        //LoadAsync<GameObject>("MyCube", LoadComplete1, Loading);
        LoadSceneAsyn("Scenes/MountainGroup", LoadComplete2, Loading);
        //StartCoroutine(CalculateCombatValue(GetResult));
    }

    
    void Update()
    {
        
    }

    private void LoadComplete1(GameObject obj)
    {
        Debug.Log("加载的资源名称：" + obj.name);
    }

    private void LoadComplete2()
    {
        Debug.Log("场景加载完成！" );
    }

    private void Loading(int p)
    {
        progressText.text = p.ToString();
        float temp = p / 100f;
        Debug.Log("temp = " + temp);
        slider.value = temp;
    }

    private void GetResult(int health)
    {
        Debug.Log("结果：" + health);
    }


    //------------------------场景加载-------------------------------------
    //说明：这里故意第一个用UnityAction，第二个用c#带Action，这两者是参数返回值方面和效率
    //有一些差别的，后者效率会更高，尽量使用Action

    /// <summary>
    /// 异步加载资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="name">资源名字</param>
    /// <param name="callback">加载完的回调,一个返回参数</param>
    /// <param name="callback2">加载中回调,一个返回参数，用来显示进度条</param>
    public void LoadAsync<T>(string name, UnityAction<T> callback, Action<float> callback2) where T : UnityEngine.Object
    {
        //开启异步加载的协程
        StartCoroutine(ReallyLoadAsync<T>(name, callback, callback2));        
    }

    private IEnumerator ReallyLoadAsync<T>(string name, UnityAction<T> callback, Action<float> callback2) where T : UnityEngine.Object
    {
        ResourceRequest r = Resources.LoadAsync<T>(name);
        if (!r.isDone)
        {
            //这里本来是应该调用r.progress到0.9的时候会自动停止更新r.progress,导致进度条到90就停了
            //需要r.allowSceneActivation = true;才能等于100.
            if (r.progress < 0.9f)
            {
                callback2(r.progress);
                yield return r;
            }
            callback2(1);
            //yield return new WaitForEndOfFrame();
            r.allowSceneActivation = true;
        }       
        yield return r;

        if (r.asset is GameObject)
        {
            //实例化一下再传给方法
            callback(GameObject.Instantiate(r.asset) as T);
        }
        else
        {
            Debug.Log(222);
            //直接传给方法
            callback(r.asset as T);
        }
    }


    //------------------------场景加载-------------------------------------
    //引入displayProgress和toProgress，目的是为了让进度条的更加平滑
    public void LoadSceneAsyn(string name, UnityAction func, Action<int> callback2)
    {       
        StartCoroutine(ReallyLoadSceneAsyn(name, func, callback2));
    }
    private IEnumerator ReallyLoadSceneAsyn(string name, UnityAction func, Action<int> callback2)
    {   
        int displayProgress = 0;
        int toProgress = 0;
        AsyncOperation ao = SceneManager.LoadSceneAsync(name);
        ao.allowSceneActivation = false;
        while (ao.progress < 0.9f)
        {
            //toProgress = (int)ao.progress * 100;           
            toProgress = 90;           
            while(displayProgress <= toProgress)
            {
                Debug.Log("------------------toProgress = " + toProgress + "-------------displayProgress = " + displayProgress);
                ++displayProgress;
                callback2(displayProgress);
                //这里故意延时0.01秒是为了放慢加载速度，更好地观察代码效果，实际应用不能这么写
                yield return new WaitForSeconds(0.02f);

                //使用下面的任一句都可以
                //yield return new WaitForEndOfFrame();
                //yield return ao.progress;
            }
        }

        toProgress = 100;
        while(displayProgress <toProgress)
        {
            ++displayProgress;
            callback2(displayProgress);

            yield return new WaitForSeconds(0.02f);
            //yield return new WaitForEndOfFrame();
            //yield return ao;
        }

        //加载完成后执行func
        func();
        ao.allowSceneActivation = true;
    }


    //------------------------应用2：战斗计算-------------------------------------
    //目前有一个应用场景是这样的，游戏中有一项费时的数据计算，
    //如果直接执行计算，再执行后面的逻辑就会造成游戏卡顿,如何解决？
    //可以把计算放在一个协程中去计算，
    //协程没有返回值，同时协程还不能传递引用,
    //可是现在需要把计算中的中间值以及最终结果（如中间值是角色血条的变化）传递出去，怎么办呢?
    //不能传递引用，可以传递数组地址（取巧），或者采用全局变量的方式。
    //这两种办法是可以解决，取巧看起来不直接，全局增加耦合，有没有更好的办法呢？
    //这时候就可以采用回调（也可以叫委托）的方式了，通过带参数的回调，把协程里的数值传递出去。
    public IEnumerator CalculateCombatValue(Action<int> callback)
    {
        int result;
        yield return new WaitForSeconds(3f);
        result = 10;
        //3秒后得到计算结果
        callback(result);
        yield return null;
    }
}
