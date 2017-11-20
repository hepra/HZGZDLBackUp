using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GZDL_DEV.model
{
   public class class_queue
    {
           List<double>[] data;  //数据元素
           int maxsize;    //最大容量
           int front;      //指向队头
           int rear;       //指向队尾
           public string Header{get;set;}
           ///
           /// 初始化队列
           ///
           ///
           public class_queue(int size)
           {
               this.maxsize = size;
               data = new List<double>[size];
               front = rear = -1;
           }
           ///
           /// 最大容量属性
           ///
           public int MaxSize
           {
               get
               {
                   return this.maxsize;
               }
               set
               {
                   this.maxsize = value;
               }
           }
           ///
           /// 队尾属性
           ///
           public int Rear
           {
               get
               {
                   return this.rear;
               }
           }
           ///
           /// 队头属性
           ///
           public int Front
           {
               get
               {
                   return this.front;
               }
           }
           ///
           /// 数据属性
           ///
           ///
           ///
           public List<double> this[int index]
           {
               get
               {
                   return data[index];
               }
           }
           ///
           /// 获得队列的长度
           ///
           ///
           public int Length()
           {
               return rear - front;
           }
           ///
           /// 判断队列是否满
           ///
           ///
           public bool IsFull()
           {
               if (Length() == maxsize)
                   return true;
               else
                   return false;
           }
           //判断队列是否为空
           public bool IsEmpty()
           {
               if (rear == front)
                   return true;
               else
                   return false;
           }
           ///
           /// 清空队列
           ///
           public void ClearQueue()
           {
               rear = front = -1;
           }
           ///
           /// 入队
           ///
           ///
           public void In(List<double> e)
           {
               data[++rear] = e;
           }

           ///
           /// 出队
           ///
           ///
           public void Out()
           {
               List<double>[] tmp = data;                
                ClearQueue();
                for (int i =0; i < tmp.Length;i++ )
                {
                    if(i+1<tmp.Length)
                    {
                        In(tmp[i + 1]);
                    }
                }
           }
    }
}
