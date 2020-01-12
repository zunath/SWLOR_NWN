import * as React from 'react';
import { Link } from 'react-router-dom';
import Logout from './Logout';
import Logo from '../images/swollogo2.png';

export default class Header extends React.Component {
    constructor(props) {
        super(props);
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
                            <li className="nav-item dropdown">
                                <Link id="loreDropdown" className="nav-link dropdown-toggle" to="#" data-toggle="dropdown" role="button">
                                    <i className="fa fa-book fa-lg" /> &nbsp;Server Info
                                </Link>
                                <div className="dropdown-menu">
                                    <Link className="dropdown-item" to="/about">
                                        <i className="fa fa-info-circle fa-lg" /> &nbsp;About
                                    </Link>
                                    <Link className="dropdown-item" to="/rules">
                                        <i className="fa fa-list-ul fa-lg" /> &nbsp;Rules
                                    </Link>
                                    <Link className="dropdown-item" to="/gallery">
                                        <i className="fa fa-image fa-lg" /> &nbsp;Gallery
                                    </Link>
                                </div>
                            </li>
                            
                            <li className="nav-item">
                                <Link className="nav-link" to="/downloads">
                                    <i className="fa fa-download fa-lg" /> Downloads
                                </Link>
                            </li>
                            <li className="nav-item dropdown">
                                <Link id="loreDropdown" className="nav-link dropdown-toggle" to="#" data-toggle="dropdown" role="button">
                                    <i className="fa fa-book fa-lg" /> &nbsp;Lore
                                </Link>
                                <div className="dropdown-menu">
                                    <Link className="dropdown-item" to="/setting">
                                        <i className="fa fa-bolt" /> &nbsp;Setting
                                    </Link>
                                    <Link className="dropdown-item" to="/species">
                                        <i className="fa fa-level-up" /> &nbsp;Species
                                    </Link>
                                </div>
                            </li>
                            <li className="nav-item dropdown">
                                <Link id="gameplayDropdown" className="nav-link dropdown-toggle" to="#" data-toggle="dropdown" role="button">
                                    <i className="fa fa-info-circle fa-lg" /> &nbsp;Gameplay
                                </Link>
                                <div className="dropdown-menu">
                                    {/* <Link className="dropdown-item" to="/skills">
                                        <i className="fa fa-bolt" /> &nbsp;Skills
                                    </Link>
                                    <Link className="dropdown-item" to="/perks">
                                        <i className="fa fa-level-up" /> &nbsp;Perks
                                    </Link> */}
                                    <Link className="dropdown-item" to="/backgrounds">
                                        <i className="fa fa-user-secret" /> &nbsp;Character Backgrounds
                                    </Link>
                                    <Link className="dropdown-item" to="/crafting">
                                        <i className="fa fa-simplybuilt" /> &nbsp;Crafting
                                    </Link>
                                    <Link className="dropdown-item" to="/gameplay-info">
                                        <i className="fa fa-compass" /> &nbsp;Other Gameplay Info
                                    </Link>
                                    <Link className="dropdown-item" to="/faq">
                                        <i className="fa fa-question" /> &nbsp;FAQ
                                    </Link>

                                </div>
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
                            <li className="nav-item">
                                <a className="nav-link" href="https://github.com/zunath/SWLOR_NWN" target="_blank" rel="nopener noreferrer">
                                    <i className="fa fa-code" /> &nbsp;Source Code
                                </a>
                            </li>                            
                        </ul>
                    </div>


                </nav>
            </div>
        );



    }
}
