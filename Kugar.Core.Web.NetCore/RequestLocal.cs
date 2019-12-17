using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using Microsoft.AspNetCore.Http;

namespace Kugar.Core.Web
{
    /// <summary>
    /// 与ThreadLocal对应的,该类的值存储在不同的Request中
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RequestLocal<T>
    {
        private string _id;
        private Func<T> _valueFactory = null;
        //private ReaderWriterLockSlim _locker=new ReaderWriterLockSlim();

        public RequestLocal()
        {
            _id = "RequestLocal" + Guid.NewGuid();
        }

        public RequestLocal(Func<T> valueFactory):this()
        {
            _valueFactory = valueFactory;
        }

        public T Value
        {
            get
            {
                var context = HttpContext.Current;

                T v;

                if (IsValueCreated)
                {
                    v= (T) context.Items[_id];
                }
                else
                {
                    try
                    {
                        if (_valueFactory != null)
                        {
                            v = _valueFactory();

                            context.Items.Add(_id, v);

                            OnValueChange();
                        }
                        else
                        {
                            v= default(T);
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                       
                    }
                }


                return v;
            }
            set
            {
                var context = HttpContext.Current;

                try
                {
                    if (IsValueCreated)
                    {
                        context.Items[_id] = value;
                    }
                    else
                    {
                        context.Items[_id] = value;
                    }

                    OnValueChange();
                }
                catch (Exception)
                {

                    throw;
                }
                finally
                {
                }

            }
        }

        public bool IsValueCreated
        {
            get
            {

                var context = HttpContext.Current;

                return context.Items.ContainsKey(_id);
            }
        }

        public void Reset()
        {
            if (IsValueCreated)
            {
                return;
            }

            HttpContext.Current.Items.Remove(_id);
        }

        private void OnValueChange()
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler ValueChanged;
    }
}
