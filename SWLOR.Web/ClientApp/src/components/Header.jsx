import * as React from 'react';
import { Link } from 'react-router-dom';
import Logo from '../images/swollogo2.png';

export default class Header extends React.Component {
    constructor(props) {
        super(props);
    }

    confirmLogout() {
    }

    render() {

        return (
            <div className="container">
                <Link className="navbar-brand" to="/">
                    <img src={Logo} alt="Star Wars: LOR" className="img-fluid" />
                </Link>
                <nav className="navbar navbar-expand-lg navbar-dark bg-dark">
                    <div className="navbar">
                        <ul className="navbar-nav">
                            <li className="nav-item">
                                <Link className="nav-link" to="/home">
                                    <i className="fa fa-home fa-lg" /> Home
                                </Link>
                            </li>
                            <li className="nav-item">
                                <Link className="nav-link" to="/about">
                                    <i className="fa fa-info-circle fa-lg" /> About
                                </Link>
                            </li>
                            <li className="nav-item">
                                <Link className="nav-link" to="/rules">
                                    <i className="fa fa-list-ul fa-lg" /> Rules
                                </Link>
                            </li>
                            <li className="nav-item">
                                <Link className="nav-link" to="/gallery">
                                    <i className="fa fa-image fa-lg" /> Gallery
                                </Link>
                            </li>
                            
                            <li className="nav-item">
                                <Link className="nav-link" to="/downloads">
                                    <i className="fa fa-download fa-lg" /> Downloads
                                </Link>
                            </li>
                            <li className="nav-item">
                                <a className="nav-link" href="https://wiki.starwarsnwn.com/">
                                    <i className="fa fa-wikipedia-w fa-lg" /> Wiki
                                </a>
                            </li>
                            <li className="nav-item">
                                <a className="nav-link" href="https://forums.starwarsnwn.com/">
                                    <i className="fa fa-forumbee fa-lg" /> Forums
                                </a>
                            </li>
                            <li className="nav-item">
                                <a className="nav-link" href="https://discord.gg/MyQAM6m" target="_blank" rel="nopener noreferrer">
                                    <i className="fa fa-commenting-o fa-lg" /> Discord
                                </a>
                            </li>

                            <li className="nav-item dropdown">
                                <Link id="serverInfoDropdown" className="nav-link dropdown-toggle" to="#" data-toggle="dropdown" role="button">
                                    <i className="fa fa-code" /> Source Code
                                </Link>
                                <div className="dropdown-menu">
                                    <a className="dropdown-item" href="https://github.com/zunath/SWLOR_NWN" target="_blank" rel="nopener noreferrer">
                                        <i className="fa fa-code" /> &nbsp;Server Source Code
                                    </a>
                                    <a className="dropdown-item" href="https://github.com/zunath/SWLOR_Website" target="_blank" rel="nopener noreferrer">
                                        <i className="fa fa-code" /> &nbsp;Website Source Code
                                    </a>
                                </div>
                            </li>
                            
                        </ul>
                    </div>


                </nav>
            </div>
        );



    }
}
