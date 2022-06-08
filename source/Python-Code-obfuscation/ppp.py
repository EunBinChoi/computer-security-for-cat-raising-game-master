import netaddr
def ip_check(ip):
    if netaddr.valid_ipv4(ip) is True or netaddr.valid_ipv6(ip) is True:
        return True
    else:
        return False