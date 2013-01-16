﻿using System;

namespace LanExchange.Strategy
{
    public abstract class AbstractSubscriptionStrategy : IBackgroundStrategy
    {
        private readonly string m_Subject;

        protected AbstractSubscriptionStrategy(string subject)
        {
            m_Subject = subject;
        }

        public abstract void Algorithm();

        public string Subject
        {
            get { return m_Subject; }
        }

        public override string ToString()
        {
            return String.Format("{0}(\"{1}\")", base.ToString(), m_Subject);
        }
    }
}