import numpy as np

def ss(d):
    nu = d - np.min(d, 0)
    de = np.max(d, 0) - np.min(d, 0)
    # noise term prevents the zero division
    return nu / (de + 1e-7)