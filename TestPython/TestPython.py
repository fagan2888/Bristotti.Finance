# always yielding a real result, even dividing two integers
from __future__ import division

import numpy as np
import matplotlib.pyplot as plt
import pandas as pd
import scipy.stats as st
import datetime as dt
from dateutil.relativedelta import relativedelta
from functools import reduce
import operator
import datetime
from scipy.optimize import minimize
import math


def calc_r_factor(r,term):
    return (1.+r/100.)**(term/252.)

def myfunction(df,r0,x0):

    fwdinput=fwdinput=pd.Series(x0)
    df['fwdinput']=fwdinput
    df['fwdinput_prev']=fwdinput.shift(1)
    i_chgfwd=df['same fwd']==False
    i_keepfwd=df['same fwd']==True
    df.loc[i_chgfwd,'fwd']=df.loc[i_chgfwd,'fwdinput']
    df.loc[i_keepfwd,'fwd']=df.loc[i_keepfwd,'fwdinput_prev']
    df.loc[0,'r']=r0
    df.loc[0,'r_factor']=(1.+df.loc[0,'r']/100.)**(df.loc[0,'term']/252.)
    df['term_delta']=df.term.diff()
    df['fwd_factor']=(1.+df['fwd']/100.)**(df['term_delta']/252.)

    for i in range(1,len(df)):
        df.loc[i,'r_factor']=df.loc[i-1,'r_factor']*df.loc[i,'fwd_factor']
        df.loc[i,'r']=(df.loc[i,'r_factor']**(252./df.loc[i,'term_delta'])-1.)*100

    return abs((df['r']-df['rm']).sum())

x0=np.array([13.10,13.20,13.30,13.40])
r0=13.37
df=pd.DataFrame([[False,312,13.37],[True,325],[False,355],[False,377,13.32]],columns=['same fwd','term','rm'])

myfunction(df,r0,fwdinput)


model_to_minimize = lambda x: myfunction(df,r0,x)
res=minimize(model_to_minimize,fwdinput,method='nelder-mead',options={'xtol': 1e-8, 'disp': True})

# function
df['fwdinput']=fwdinput
df['fwdinput_prev']=fwdinput.shift(1)
i_chgfwd=df['same fwd']==False
i_keepfwd=df['same fwd']==True
df.loc[i_chgfwd,'fwd']=df.loc[i_chgfwd,'fwdinput']
df.loc[i_keepfwd,'fwd']=df.loc[i_keepfwd,'fwdinput_prev']
df.loc[0,'r']=r0
df.loc[0,'r_factor']=(1.+df.loc[0,'r']/100.)**(df.loc[0,'term']/252.)
df['term_delta']=df.term.diff()
df['fwd_factor']=(1.+df['fwd']/100.)**(df['term_delta']/252.)
